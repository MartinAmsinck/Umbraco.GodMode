﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Diplo.GodMode.Helpers;
using Umbraco.Core;

namespace Diplo.GodMode.Models
{
    /// <summary>
    /// Represents a repeatable diagnostic section
    /// </summary>
    public class DiagnosticSection
    {
        public DiagnosticSection(string heading)
        {
            this.Heading = heading;
            this.Diagnostics = new List<Diagnostic>();
        }

        public DiagnosticSection(string heading, IEnumerable<Diagnostic> diagnostics) : this(heading)
        {
            this.Diagnostics = diagnostics.ToList();
        }

        public string Heading { get; set; }

        public List<Diagnostic> Diagnostics { get; set; }

        public void AddDiagnostics(NameValueCollection nvc, bool skipEmpty = true, Func<string, bool> predicate = null)
        {
            if (nvc != null)
            {
                var keys = nvc.AllKeys;

                if (predicate != null)
                    keys = keys.Where(predicate).ToArray();

                foreach (var key in keys)
                {
                    if (skipEmpty && !String.IsNullOrEmpty(nvc[key]) || !skipEmpty)
                    {
                        this.Diagnostics.Add(new Diagnostic(key, nvc[key]));
                    }
                }
            }
        }

        public void AddDiagnostics(IDictionary<object, object> items)
        {
            if (items != null)
            {
                foreach (var key in items.Keys)
                {
                    this.Diagnostics.Add(new Diagnostic(key.ToString(), items[key].ToString()));
                }
            }
        }

        public void AddDiagnosticsFrom(object obj, bool onlyUmbraco = true)
        {
            if (obj != null)
            {
                this.Diagnostics.AddRange(ReflectionHelper.PopulateDiagnosticsFrom(obj, onlyUmbraco));
            }
        }

        public void AddDiagnosticsFrom(Type type)
        {
            if (type != null)
            {
                foreach (var item in ReflectionHelper.GetTypesAssignableFrom(type))
                {
                    this.Diagnostics.Add(new Diagnostic(item.Name, item.GetFullNameWithAssembly()));
                }
            }
        }
    }
}