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

using System.Diagnostics.CodeAnalysis;

namespace Yuma.Persistence;

/// <summary>Represents a factory for creating unit of work strategy instances.</summary>
/// <remarks>
/// This interface defines a contract for creating instances of <see cref="IUnitOfWorkStrategy"/>, allowing for flexible
/// and configurable unit of work management in persistence operations.
/// </remarks>
[SuppressMessage("ReSharper", "MemberCanBeInternal", Justification = "Public API.")]
public interface IUnitOfWorkStrategyFactory
{
	/// <summary>Creates and returns a new instance of a unit of work strategy.</summary>
	/// <returns>An implementation of <see cref="IUnitOfWorkStrategy"/> that defines the behavior for managing unit of work operations.</returns>
	/// <remarks>
	/// The returned strategy can be used to manage transactions, track changes, and control the persistence lifecycle of
	/// database operations.
	/// </remarks>
	[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Public API.")]
	IUnitOfWorkStrategy Create();
}
