#region Copyright & License

// Copyright © 2024 - 2025 Yuma
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Yuma.Persistence;

/// <summary>
/// Base class for an <see cref="IUnitOfWork"/> implementation. If <see cref="Dispose()"/> is called without calling
/// <see cref="CommitAsync"/> before, the underlying transaction will be rolled back.
/// </summary>
[SuppressMessage("ReSharper", "MemberCanBeInternal", Justification = "Public API.")]
public abstract class AbstractUnitOfWork : IUnitOfWork
{
	#region IUnitOfWork Members

	/// <inheritdoc/>
	public abstract Task FlushAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Commits all changes made within the unit of work to the underlying resource managers and executes registered
	/// callbacks.
	/// </summary>
	/// <param name="cancellationToken">A cancellation token that can be used to observe cancellation requests.</param>
	/// <returns>A task that represents the asynchronous commit operation.</returns>
	/// <remarks>
	/// Follows a sequential commit algorithm: <list type="number">
	/// <item><description>Ensures resources are flushed via <see cref="FlushAsync"/></description></item>
	/// <item><description>Executes pre-commit callbacks to prepare for commit</description></item>
	/// <item><description>Performs core commit operation</description></item>
	/// <item><description>Marks unit of work as completed</description></item>
	/// <item><description>Triggers post-commit callbacks for final processing</description></item>
	/// </list>
	/// </remarks>
	public async Task CommitAsync(CancellationToken cancellationToken = default)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		if (_completed) throw new InvalidOperationException("This unit of work has already committed.");

		await FlushAsync(cancellationToken);
		await InvokeCallbacks(_beforeCommitCallbacks);
		await CommitAsyncCore(cancellationToken);
		_completed = true;
		await InvokeCallbacks(_afterCommitCallbacks);
	}

	/// <inheritdoc/>
	public abstract DbTransaction? DbTransaction { get; }

	/// <inheritdoc/>
	public void RegisterBeforeCommitCallback(Func<IUnitOfWork, Task> callback)
	{
		ArgumentNullException.ThrowIfNull(callback);
		_beforeCommitCallbacks.Add(callback);
	}

	/// <inheritdoc/>
	public void RegisterAfterCommitCallback(Func<IUnitOfWork, Task> callback)
	{
		ArgumentNullException.ThrowIfNull(callback);
		_afterCommitCallbacks.Add(callback);
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	#endregion

	/// <summary>
	/// Releases all resources used by the current instance of the <see cref="AbstractUnitOfWork"/> class. If
	/// <see cref="Dispose"/> is called before <see cref="CommitAsync"/>, the transaction will be rolled back.
	/// </summary>
	/// <param name="disposing">
	/// A boolean value indicating whether the method should release both managed and unmanaged resources. If
	/// true, both managed and unmanaged resources are disposed of; otherwise, only unmanaged resources are released.
	/// </param>
	[SuppressMessage("ReSharper", "VirtualMemberNeverOverridden.Global", Justification = "Dispose pattern public API.")]
	protected virtual void Dispose(bool disposing)
	{
		if (_disposed) return;
		if (disposing)
		{
			_beforeCommitCallbacks.Clear();
			_afterCommitCallbacks.Clear();
		}
		_completed = true;
		_disposed = true;
	}

	/// <summary>
	/// Executes the necessary logic to commit the current unit of work core operation. This method should be implemented by
	/// derived classes to handle specific persistence mechanisms during the commit process.
	/// </summary>
	/// <param name="cancellationToken">A token that can be used to signal the operation should be canceled.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	[SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = "Async pattern cancellation token.")]
	protected internal abstract Task CommitAsyncCore(CancellationToken cancellationToken = default);

	[SuppressMessage("ReSharper", "ForCanBeConvertedToForeach", Justification = "To support list mutation while iterating.")]
	private async Task InvokeCallbacks(IList<Func<IUnitOfWork, Task>> callbacks)
	{
		// Use index-based enumeration to support a dynamic scenario where callbacks can register additional callbacks during iteration.
		// Using an `IEnumerable` would cause iterator modification, triggering an `InvalidOperationException`
		for (var i = 0; i < callbacks.Count; i++)
		{
			var commitCallback = callbacks[i];
			await commitCallback(this);
		}
	}

	private readonly IList<Func<IUnitOfWork, Task>> _afterCommitCallbacks = [];
	private readonly IList<Func<IUnitOfWork, Task>> _beforeCommitCallbacks = [];
	private bool _completed;
	private bool _disposed;
}
