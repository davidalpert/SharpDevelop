﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using ICSharpCode.PackageManagement;
using ICSharpCode.PackageManagement.Design;
using NuGet;

namespace PackageManagement.Tests.Helpers
{
	public class TestablePackageViewModel : PackageViewModel
	{
		public FakePackageManagementSolution FakeSolution;
		public FakePackageManagementEvents FakePackageManagementEvents;
		public FakePackage FakePackage;
		public FakeLogger FakeLogger;
		public FakePackageActionRunner FakeActionRunner;
		
		public TestablePackageViewModel(FakePackageManagementSolution solution)
			: this(
				new FakePackage(),
				solution,
				new FakePackageManagementEvents(),
				new FakePackageActionRunner(),
				new FakeLogger())
		{
		}
		
		public TestablePackageViewModel(
			FakePackage package,
			FakePackageManagementSolution solution,
			FakePackageManagementEvents packageManagementEvents,
			FakePackageActionRunner actionRunner,
			FakeLogger logger)
			: base(
				package,
				solution,
				packageManagementEvents,
				actionRunner,
				logger)
		{
			this.FakePackage = package;
			this.FakeSolution = solution;
			this.FakePackageManagementEvents = packageManagementEvents;
			this.FakeActionRunner = actionRunner;
			this.FakeLogger = logger;
		}
		
		protected override PackageViewModelOperationLogger CreateLogger(ILogger logger)
		{
			PackageViewModelOperationLogger operationLogger = base.CreateLogger(logger);
			operationLogger.AddingPackageMessageFormat = "Installing...{0}";
			operationLogger.RemovingPackageMessageFormat = "Uninstalling...{0}";
			OperationLoggerCreated = operationLogger;
			return operationLogger;
		}
		
		public PackageViewModelOperationLogger OperationLoggerCreated;
		
		public PackageOperation AddOneFakeInstallPackageOperationForViewModelPackage()
		{
			var operation = new PackageOperation(FakePackage, PackageAction.Install);
			
			FakeSolution
				.FakeActiveProject
				.FakeInstallOperations
				.Add(operation);
			
			return operation;
		}
		
		public PackageOperation AddOneFakeUninstallPackageOperation()
		{
			var package = new FakePackage("PackageToUninstall");			
			var operation = new PackageOperation(package, PackageAction.Uninstall);
			FakeSolution.FakeActiveProject.FakeInstallOperations.Add(operation);
			return operation;
		}
	}
}
