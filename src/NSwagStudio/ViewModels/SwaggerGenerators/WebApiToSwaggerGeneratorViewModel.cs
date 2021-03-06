﻿//-----------------------------------------------------------------------
// <copyright file="WebApiToSwaggerGeneratorViewModel.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/NSwag/NSwag/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;
using MyToolkit.Command;
using NJsonSchema;
using NSwag.CodeGeneration.Commands;
using NSwag.CodeGeneration.SwaggerGenerators.WebApi;

namespace NSwagStudio.ViewModels.SwaggerGenerators
{
    public class WebApiToSwaggerGeneratorViewModel : ViewModelBase
    {
        private string[] _allControllerNames = { };
        private WebApiToSwaggerCommand _command = new WebApiToSwaggerCommand();

        /// <summary>Initializes a new instance of the <see cref="WebApiToSwaggerGeneratorViewModel"/> class.</summary>
        public WebApiToSwaggerGeneratorViewModel()
        {
            BrowseAssemblyCommand = new AsyncRelayCommand(BrowseAssembly);

            LoadAssembliesCommand = new AsyncRelayCommand(async () => await LoadAssembliesAsync(), () => AssemblyPaths?.Length > 0);
            LoadAssembliesCommand.TryExecute();
        }

        /// <summary>Gets the default enum handlings. </summary>
        public EnumHandling[] EnumHandlings
        {
            get { return Enum.GetNames(typeof(EnumHandling)).Select(t => (EnumHandling)Enum.Parse(typeof(EnumHandling), t)).ToArray(); }
        }

        /// <summary>Gets the default property name handlings. </summary>
        public PropertyNameHandling[] PropertyNameHandlings
        {
            get { return Enum.GetNames(typeof(PropertyNameHandling)).Select(t => (PropertyNameHandling)Enum.Parse(typeof(PropertyNameHandling), t)).ToArray(); }
        }

        /// <summary>Gets or sets the command to browse for an assembly.</summary>
        public AsyncRelayCommand BrowseAssemblyCommand { get; set; }

        /// <summary>Gets or sets the command to load the controller types from an assembly.</summary>
        public AsyncRelayCommand LoadAssembliesCommand { get; set; }

        /// <summary>Gets or sets the generator settings. </summary>
        public WebApiToSwaggerCommand Command
        {
            get { return _command; }
            set
            {
                if (Set(ref _command, value))
                {
                    RaiseAllPropertiesChanged();
                    LoadAssembliesCommand.RaiseCanExecuteChanged();
                    LoadAssembliesAsync();
                }
            }
        }

        /// <summary>Gets or sets the assembly path. </summary>
        public string[] AssemblyPaths
        {
            get { return Command.AssemblyPaths; }
            set
            {
                Command.AssemblyPaths = value;
                LoadAssembliesCommand.RaiseCanExecuteChanged();
                RaisePropertyChanged(() => AssemblyPaths);
            }
        }

        /// <summary>Gets or sets the class name. </summary>
        public IEnumerable<string> ControllerNames
        {
            get { return Command.ControllerNames; }
            set
            {
                Command.ControllerNames = value.ToArray();
                RaisePropertyChanged(() => ControllerNames);
            }
        }

        /// <summary>Gets or sets the all class names. </summary>
        public string[] AllControllerNames
        {
            get { return _allControllerNames; }
            set { Set(ref _allControllerNames, value); }
        }

        public async Task<string> GenerateSwaggerAsync()
        {
            return await RunTaskAsync(async () => (await Command.RunAsync())?.ToJson());
        }

        private async Task BrowseAssembly()
        {
            var dlg = new OpenFileDialog();
            dlg.DefaultExt = ".dll"; // 
            dlg.Filter = ".NET Assemblies (.dll)|*.dll";
            if (dlg.ShowDialog() == true)
            {
                AssemblyPaths = new [] { dlg.FileName };
                await LoadAssembliesAsync();
            }
        }

        private Task LoadAssembliesAsync()
        {
            return RunTaskAsync(async () =>
            {
                AllControllerNames = await Task.Run(() =>
                {
                    if (Command.Settings.AssemblyPaths?.Length > 0)
                    {
                        var generator = new WebApiAssemblyToSwaggerGenerator(Command.Settings);
                        return generator.GetControllerClasses();
                    }
                    else
                        return new string[] { };
                });

                if (ControllerNames != null)
                {
                    var newControllerNames = ControllerNames.ToList();
                    foreach (var controller in newControllerNames.ToArray())
                    {
                        if (!AllControllerNames.Contains(controller))
                            newControllerNames.Remove(controller);
                    }
                    ControllerNames = newControllerNames.ToArray();
                }
                else
                    ControllerNames = new string[] { };
            });
        }
    }
}
