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
using Moq;

namespace Yuma.Persistence;

public abstract class AbstractUnitOfWorkFixture
{
	#region Nested Type: CommitAsync

	public class CommitAsync : AbstractUnitOfWorkFixture
	{
		[Fact]
		public async Task CallsMethodsInExpectedOrder()
		{
			var unitOfWorkMock = new Mock<AbstractUnitOfWorkDummy>(MockBehavior.Strict) {
				CallBase = true
			};
			var sequence = new MockSequence();
			unitOfWorkMock.InSequence(sequence)
				.Setup(static uow => uow.FlushAsync(It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask)
				.Verifiable();
			unitOfWorkMock.InSequence(sequence)
				.Setup(static uow => uow.BeforeCommit(It.IsAny<IUnitOfWork>()))
				.Returns(Task.CompletedTask)
				.Verifiable();
			unitOfWorkMock.InSequence(sequence)
				.Setup(static uow => uow.CommitAsyncCore(It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask)
				.Verifiable();
			unitOfWorkMock.InSequence(sequence)
				.Setup(static uow => uow.AfterCommit(It.IsAny<IUnitOfWork>()))
				.Returns(Task.CompletedTask)
				.Verifiable();

			await unitOfWorkMock.Object.CommitAsync();

			unitOfWorkMock.Verify(static uow => uow.FlushAsync(It.IsAny<CancellationToken>()), Times.Once);
			unitOfWorkMock.Verify(static uow => uow.BeforeCommit(It.IsAny<IUnitOfWork>()), Times.Once);
			unitOfWorkMock.Verify(static uow => uow.CommitAsyncCore(It.IsAny<CancellationToken>()), Times.Once);
			unitOfWorkMock.Verify(static uow => uow.AfterCommit(It.IsAny<IUnitOfWork>()), Times.Once);
		}
	}

	#endregion

	#region Nested Type: CommitAsyncThrows

	public class CommitAsyncThrows : AbstractUnitOfWorkFixture
	{
		[SuppressMessage("ReSharper", "AccessToDisposedClosure")]
		[Fact]
		public async Task ThrowsWhenAlreadyCommitted()
		{
			using var unitOfWork = new AbstractUnitOfWorkFake();
			await unitOfWork.CommitAsync();
			await Invoking(async () => await unitOfWork.CommitAsync())
				.Should()
				.ThrowAsync<InvalidOperationException>()
				.WithMessage("This unit of work has already committed.");
		}

		[Fact]
		public async Task ThrowsWhenCalledAfterDispose()
		{
			var unitOfWork = new AbstractUnitOfWorkFake();
			unitOfWork.Dispose();
			await Invoking(async () => await unitOfWork.CommitAsync())
				.Should()
				.ThrowAsync<ObjectDisposedException>();
		}
	}

	#endregion

	#region Nested Type: Dispose

	public class Dispose : AbstractUnitOfWorkFixture
	{
		[Fact]
		public void DisposeCanBeCalledMultipleTimes()
		{
			var unitOfWork = new AbstractUnitOfWorkFake();
			unitOfWork.Dispose();
			Invoking(() => unitOfWork.Dispose())
				.Should()
				.NotThrow();
		}
	}

	#endregion

	#region Nested Type: AbstractUnitOfWorkDummy

	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "For mocking purposes.")]
	internal abstract class AbstractUnitOfWorkDummy : AbstractUnitOfWork
	{
		protected AbstractUnitOfWorkDummy()
		{
			RegisterAfterCommitCallback(AfterCommit);
			RegisterBeforeCommitCallback(BeforeCommit);
		}

		#region Base Class Member Overrides

		protected internal abstract override Task CommitAsyncCore(CancellationToken cancellationToken = default);

		[SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty")]
		public override DbTransaction? DbTransaction { get; }

		public override Task FlushAsync(CancellationToken cancellationToken = default)
		{
			return Task.CompletedTask;
		}

		#endregion

		internal abstract Task AfterCommit(IUnitOfWork unitOfWork);

		internal abstract Task BeforeCommit(IUnitOfWork unitOfWork);
	}

	#endregion

	#region Nested Type: AbstractUnitOfWorkFake

	private sealed class AbstractUnitOfWorkFake : AbstractUnitOfWork
	{
		#region Base Class Member Overrides

		protected internal override Task CommitAsyncCore(CancellationToken cancellationToken = default)
		{
			return Task.CompletedTask;
		}

		[SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty")]
		public override DbTransaction? DbTransaction { get; }

		public override Task FlushAsync(CancellationToken cancellationToken = default)
		{
			return Task.CompletedTask;
		}

		#endregion
	}

	#endregion
}
