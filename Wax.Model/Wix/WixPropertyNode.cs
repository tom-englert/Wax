﻿using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using tomenglertde.Wax.Model.Tools;

namespace tomenglertde.Wax.Model.Wix
{
    public class WixProperty
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class WixPropertyNode : WixNode
    {
        public WixPropertyNode([NotNull] WixSourceFile sourceFile, [NotNull] XElement node)
            : base(sourceFile, node)
        {
            Contract.Requires(sourceFile != null);
            Contract.Requires(node != null);
        }

        public WixProperty Property
        {
            get
            {
                return new WixProperty { Name = this.Name, Value = this.GetAttribute("Value") };
            }
        }
        public void Remove()
        {
            var parentNode = Node.Parent;

            Node.RemoveSelfAndWhiteSpace();
            
            SourceFile.Save();
        }

    }
}
