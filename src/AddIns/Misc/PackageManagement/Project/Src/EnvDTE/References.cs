﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections;
using System.Collections.Generic;
using ICSharpCode.SharpDevelop.Project;

namespace ICSharpCode.PackageManagement.EnvDTE
{
	public class References : IEnumerable<Reference>
	{
		MSBuildBasedProject msbuildProject;
		IPackageManagementProjectService projectService;
		Project project;
		
		public References(MSBuildBasedProject project)
			: this(project, new PackageManagementProjectService())
		{
		}
		
		public References(
			MSBuildBasedProject project,
			IPackageManagementProjectService projectService)
		{
			this.msbuildProject = project;
			this.projectService = projectService;
		}
		
		public References(Project project)
		{
			this.project = project;
		}
		
		public void Add(string path)
		{
			project.AddReference(path);
			project.Save();
		}
		
		void SaveProject()
		{
			project.Save();
		}
		
		public IEnumerator<Reference> GetEnumerator()
		{
			foreach (ReferenceProjectItem referenceProjectItem in project.GetReferences()) {
				yield return new Reference(project, referenceProjectItem);
			}
		}
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		
		public Reference Item(string identity)
		{
			return Find(identity);
		}
		
		public Reference Find(string identity)
		{
			foreach (Reference reference in this) {
				if (IsMatch(reference, identity)) {
					return reference;
				}
			}
			return null;
		}
		
		bool IsMatch(Reference reference, string identity)
		{
			return String.Equals(reference.Name, identity, StringComparison.InvariantCultureIgnoreCase);
		}
		
		/// <summary>
		/// This method should be in a separate AssemblyReferences class that is exposed by web projects.
		/// </summary>
		public void AddFromGAC(string assemblyName)
		{
			Add(assemblyName);
		}
	}
}
