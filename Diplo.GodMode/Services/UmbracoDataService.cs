﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Diplo.GodMode.Controllers;
using Diplo.GodMode.Helpers;
using Diplo.GodMode.Models;
using Umbraco.Core;
using Umbraco.Core.Services;
using Umbraco.Web;

namespace Diplo.GodMode.Services
{
    /// <summary>
    /// Used to get data out of Umbraco
    /// </summary>
    public class UmbracoDataService
    {
        private UmbracoHelper umbHelper;

        private UmbracoContext umbContext;

        private ServiceContext services;

        public UmbracoDataService(UmbracoHelper umbHelper)
        {
            this.umbHelper = umbHelper;
            this.umbContext = umbHelper.UmbracoContext;
            this.services = umbContext.Application.Services;
        }

        public IEnumerable<ContentTypeMap> GetContentTypeMap()
        {
            var cts = services.ContentTypeService;

            List<ContentTypeMap> mapping = new List<ContentTypeMap>();

            foreach (var ct in cts.GetAllContentTypes().OrderBy(x => x.Name))
            {
                ContentTypeMap map = new ContentTypeMap();

                map.Alias = ct.Alias;
                map.Icon = ct.Icon;
                map.Name = ct.Name;
                map.Id = ct.Id;
                map.Description = ct.Description;
                map.Templates = ct.AllowedTemplates.
                Select(x => new TemplateMap()
                {
                    Alias = x.Alias,
                    Id = x.Id,
                    Name = x.Name,
                    Path = x.VirtualPath,
                    IsDefault = ct.DefaultTemplate.Id == x.Id
                });

                map.Properties = ct.PropertyTypes.Select(p => new PropertyTypeMap(p));
                map.CompositionProperties = ct.CompositionPropertyTypes.Where(p => !ct.PropertyTypes.Select(x => x.Id).Contains(p.Id)).Select(pt => new PropertyTypeMap(pt));
                map.Compositions = ct.ContentTypeComposition.
                Select(x => new ContentTypeData()
                {
                    Alias = x.Alias,
                    Description = x.Description,
                    Id = x.Id,
                    Icon = x.Icon,
                    Name = x.Name
                });

                map.AllProperties = map.Properties.Concat(map.CompositionProperties);
                map.HasCompositions = ct.ContentTypeComposition.Any();
                map.HasTemplates = ct.AllowedTemplates.Any();
                map.IsListView = ct.IsContainer;
                map.AllowedAtRoot = ct.AllowedAsRoot;
                map.PropertyGroups = ct.PropertyGroups.Select(x => x.Name);

                mapping.Add(map);
            }

            return mapping;
        }

        /// <summary>
        /// Gets all property groups
        /// </summary>
        public IEnumerable<string> GetPropertyGroups()
        {
            var cts = services.ContentTypeService;
            return cts.GetAllContentTypes().
                SelectMany(x => x.PropertyGroups.Select(p => p.Name)).
                Distinct().OrderBy(p => p);
        }

        /// <summary>
        /// Gets all compositions
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ContentTypeData> GetCompositions()
        {
            var cts = services.ContentTypeService;
            return cts.GetAllContentTypes().
                SelectMany(x => x.ContentTypeComposition).
                DistinctBy(x => x.Id).
                Select(c => new ContentTypeData() { Id = c.Id, Alias = c.Alias, Name = c.Name }).
                OrderBy(x => x.Name);
        }

        /// <summary>
        /// Gets all data types
        /// </summary>
        public IEnumerable<DataTypeMap> GetDataTypes()
        {
            var dts = services.DataTypeService;
            return dts.GetAllDataTypeDefinitions().
                Select(x => new DataTypeMap() { Id = x.Id, Name = x.Name }).
                OrderBy(x => x.Name);
        }

        /// <summary>
        /// Gets all property editors
        /// </summary>
        public IEnumerable<DataTypeMap> GetPropertyEditors()
        {
            var dts = services.DataTypeService;
            return dts.GetAllDataTypeDefinitions().
                Select(x => new DataTypeMap() { Id = x.Id, Alias = x.PropertyEditorAlias }).
                DistinctBy(p => p.Alias).
                OrderBy(p => p.Alias);
        }

        /// <summary>
        /// Gets all data types, including the status of whether they are being used
        /// </summary>
        /// <returns></returns>
        public IEnumerable<DataTypeMap> GetDataTypesStatus()
        {
            var cts = services.ContentTypeService;
            var dts = services.DataTypeService;

            var dataTypes = dts.GetAllDataTypeDefinitions();
            var contentTypes = cts.GetAllContentTypes();
            var usedPropertyTypes = contentTypes.SelectMany(x => x.PropertyTypes.Concat(x.CompositionPropertyTypes));
            var usedIds = dataTypes.Where(x => usedPropertyTypes.Select(y => y.DataTypeDefinitionId).Contains(x.Id)).Select(d => d.Id);

            return dts.GetAllDataTypeDefinitions().Select(x => new DataTypeMap()
            {
                Id = x.Id,
                Name = x.Name,
                Alias = x.PropertyEditorAlias,
                DbType = x.DatabaseType.ToString(),
                IsUsed = usedIds.Contains(x.Id)
            }).
            OrderBy(x => x.Name);
        }

        /// <summary>
        /// Gets all templates
        /// </summary>
        public IEnumerable<TemplateModel> GetTemplates()
        {
            var fs = services.FileService;
            var templateModels = new List<TemplateModel>();
            var templates = fs.GetTemplates();

            foreach (var template in templates)
            {
                TemplateModel model = new TemplateModel(template)
                {
                    IsMaster = template.IsMasterTemplate,
                    MasterAlias = template.MasterTemplateAlias,
                    Partials = PartialHelper.GetPartialInfo(template.Content, template.Id, template.Alias),
                    Path = template.Path,
                    Parents = templates.Where(t => template.Path.Split(',').Select(x => Convert.ToInt32(x)).Contains(t.Id)).Select(t => new TemplateModel(t)).Reverse()
                };

                templateModels.Add(model);
            }

            return templateModels;
        }

        /// <summary>
        /// Gets all media (this can potentially return a lot - add pagination??)
        /// </summary>
        public IEnumerable<MediaMap> GetMedia()
        {
            var ms = services.MediaService;
            List<MediaMap> mediaMap = new List<MediaMap>();
            var rootMedia = ms.GetRootMedia();
            string tempExt;

            foreach (var m in rootMedia)
            {
                mediaMap.AddRange(ms.GetDescendants(m).
                    Select(x => new MediaMap()
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Alias = x.ContentType.Name,
                        Ext = tempExt = FileTypeHelper.GetExtensionFromMedia(x),
                        Type = FileTypeHelper.GetFileTypeName(tempExt),
                        Size = FileTypeHelper.GetFileSize(x)
                    }
                ));
            }

            return mediaMap;
        }
    }
}
