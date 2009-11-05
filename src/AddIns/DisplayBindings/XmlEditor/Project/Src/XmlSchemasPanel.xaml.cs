﻿// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Matthew Ward" email="mrward@users.sourceforge.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.XmlEditor;

namespace ICSharpCode.XmlEditor
{
	public partial class XmlSchemasPanel : UserControl, IOptionPanel, IXmlSchemasPanel
	{
		XmlSchemaCompletionDataCollection predefinedSchemas;
		ICollection<string> xmlFileExtensions;
		XmlEditorOptions xmlEditorOptions;
		XmlSchemaCompletionDataCollection addedSchemas = new XmlSchemaCompletionDataCollection();
		StringCollection removedSchemaNamespaces = new StringCollection();
		RegisteredXmlSchemasEditor editor;
		
		public XmlSchemasPanel() 
			: this(XmlEditorService.XmlSchemaManager,
				new DefaultXmlFileExtensions(),
				XmlEditorService.XmlEditorOptions,
				XmlEditorService.XmlSchemaManager)
		{
		}

		public XmlSchemasPanel(XmlSchemaManager schemaManager, 
			ICollection<string> xmlFileExtensions, 
			XmlEditorOptions xmlEditorOptions,
			IXmlSchemaCompletionDataFactory factory)
		{
			this.predefinedSchemas = schemaManager.Schemas;
			this.xmlFileExtensions = xmlFileExtensions;
			this.xmlEditorOptions = xmlEditorOptions;
			
			InitializeComponent();
			
			editor = new RegisteredXmlSchemasEditor(schemaManager, xmlFileExtensions, xmlEditorOptions, this, factory);
		}
		
		public object Owner { get; set; }
		
		public object Control {
			get { return this; }
		}
		
		public void LoadOptions()
		{
			editor.LoadOptions();
		}		
				
		/// <summary>
		/// Saves any changes to the configured schemas.
		/// </summary>
		public bool SaveOptions()
		{
			return editor.SaveOptions();
		}
		
		void FileExtensionComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			editor.XmlSchemaFileAssociationFileExtensionSelectionChanged();
		}
		
		/// <summary>
		/// Enables the remove button if a list box item is selected.
		/// </summary>
		void SchemaListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			editor.SchemaListSelectionChanged();
		}
		
		void AddSchemaButtonClick(object sender, RoutedEventArgs e)
		{
			editor.AddSchemaFromFileSystem();
		}
		
		void RemoveSchemaButtonClick(object sender, RoutedEventArgs e)
		{
			editor.RemoveSelectedSchema();
		}
		
		void NamespacePrefixTextBoxTextChanged(object sender, TextChangedEventArgs e)
		{
			editor.SchemaNamespacePrefixChanged();
		}
		
		void ChangeSchemaButtonClick(object sender, RoutedEventArgs e)
		{
			editor.ChangeSchemaAssociation();
		}
			
		public int XmlSchemaListItemCount {
			get { return schemaListBox.Items.Count; }
		}
		
		public int SelectedXmlSchemaListItemIndex {
			get { return schemaListBox.SelectedIndex; }
			set { schemaListBox.SelectedIndex = value; }
		}
		
		public int SelectedXmlSchemaFileAssociationListItemIndex {
			get { return fileExtensionComboBox.SelectedIndex; }
			set { fileExtensionComboBox.SelectedIndex = value; }
		}
		
		public int XmlSchemaFileAssociationListItemCount {
			get { return fileExtensionComboBox.Items.Count; }
		}
		
		public bool RemoveSchemaButtonEnabled {
			get { return removeSchemaButton.IsEnabled; }
			set {removeSchemaButton.IsEnabled = value; }
		}
		
		public XmlSchemaListItem GetXmlSchemaListItem(int index)
		{
			return (XmlSchemaListItem)schemaListBox.Items[index];
		}
		
		public int AddXmlSchemaListItem(XmlSchemaListItem schemaListItem)
		{
			return schemaListBox.Items.Add(schemaListItem);
		}
		
		public void AddXmlSchemaListSortDescription(string propertyName, ListSortDirection sortDirection)
		{
			schemaListBox.Items.SortDescriptions.Add(new SortDescription(propertyName, sortDirection));
		}
		
		public XmlSchemaListItem GetSelectedXmlSchemaListItem()
		{
			return (XmlSchemaListItem)schemaListBox.SelectedItem;
		}
		
		public void RemoveXmlSchemaListItem(XmlSchemaListItem schemaListItem)
		{
			schemaListBox.Items.Remove(schemaListItem);
		}
		
		public void RefreshXmlSchemaListItems()
		{
		}
		
		public XmlSchemaFileAssociationListItem GetXmlSchemaFileAssociationListItem(int index)
		{
			return (XmlSchemaFileAssociationListItem)fileExtensionComboBox.Items[index];
		}
		
		public XmlSchemaFileAssociationListItem GetSelectedXmlSchemaFileAssociationListItem()
		{
			return (XmlSchemaFileAssociationListItem)fileExtensionComboBox.SelectedItem;
		}
		
		public int AddXmlSchemaFileAssociationListItem(XmlSchemaFileAssociationListItem schemaFileAssociationListItem)
		{
			return fileExtensionComboBox.Items.Add(schemaFileAssociationListItem);
		}
		
		public void AddXmlSchemaFileAssociationListSortDescription(string propertyName, ListSortDirection sortDirection)
		{
			fileExtensionComboBox.Items.SortDescriptions.Add(new SortDescription(propertyName, sortDirection));
		}
		
		public string GetSelectedSchemaNamespace()
		{
			return schemaNamespaceTextBox.Text;
		}
		
		public void SetSelectedSchemaNamespace(string schemaNamespace)
		{
			schemaNamespaceTextBox.Text = schemaNamespace;
		}
		
		public string GetSelectedSchemaNamespacePrefix()
		{
			return namespacePrefixTextBox.Text;
		}
		
		public void SetSelectedSchemaNamespacePrefix(string namespacePrefix)
		{
			namespacePrefixTextBox.Text = namespacePrefix;
		}
		
		public Nullable<bool> ShowDialog(SelectXmlSchemaWindow dialog)
		{
			return dialog.ShowDialog();
		}

		public Nullable<bool> ShowDialog(OpenFileDialog dialog)
		{
			return dialog.ShowDialog();
		}
		
		public void ShowErrorFormatted(string format, string parameter)
		{
			MessageService.ShowErrorFormatted(format, parameter);
		}
		
		public void ShowExceptionError(Exception ex, string message)
		{
			MessageService.ShowException(ex, message);
		}
		
		public void ShowError(string message)
		{
			MessageService.ShowError(message);
		}
		
		public void ScrollXmlSchemaListItemIntoView(XmlSchemaListItem schemaListItem)
		{
			schemaListBox.ScrollIntoView(schemaListItem);
		}
	}
}