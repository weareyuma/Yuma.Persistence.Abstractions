#region Copyright & License

// Copyright © 2024-2025 Yuma
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
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Yuma.Persistence;

/// <summary>
/// Defines an abstraction for a unit of work pattern, providing mechanisms to coordinate, persist, and finalize
/// operations across transaction boundaries.
/// </summary>
[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Public API.")]
public interface IUnitOfWork : IDisposable
{
	/// <summary>Gets the underlying database transaction associated with the current unit of work.</summary>
	/// <remarks>
	/// This property provides access to the database transaction that is being used to manage operations within the
	/// transaction scope. If no transaction is currently in progress, this property may return null.
	/// </remarks>
	DbTransaction? DbTransaction { get; }

	/// <summary>Synchronizes all changes made within the unit of work to the resource managers involved.</summary>
	/// <param name="cancellationToken">A cancellation token that can be used to observe cancellation requests.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	[SuppressMessage("ReSharper", "UnusedMemberInSuper.Global", Justification = "Public API.")]
	[SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = "Async pattern cancellation token.")]
	Task FlushAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Commits all changes made within the unit of work to the underlying resource managers and executes registered
	/// callbacks.
	/// </summary>
	/// <param name="cancellationToken">A cancellation token that can be used to observe cancellation requests.</param>
	/// <returns>A task that represents the asynchronous commit operation.</returns>
	[SuppressMessage("ReSharper", "UnusedMemberInSuper.Global", Justification = "Public API.")]
	Task CommitAsync(CancellationToken cancellationToken = default);

	/// <summary>Registers a callback to be executed before the commit operation completes.</summary>
	/// <param name="callback">A function that represents the asynchronous callback to be executed before committing changes.</param>
	void RegisterBeforeCommitCallback(Func<IUnitOfWork, Task> callback);

	/// <summary>Registers a callback to be executed after the commit operation completes successfully.</summary>
	/// <param name="callback">A function that represents the asynchronous callback to be executed after committing changes.</param>
	void RegisterAfterCommitCallback(Func<IUnitOfWork, Task> callback);
}
