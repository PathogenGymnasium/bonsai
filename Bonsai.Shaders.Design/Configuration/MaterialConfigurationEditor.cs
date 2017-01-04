﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.Design;

namespace Bonsai.Shaders.Configuration.Design
{
    class MaterialConfigurationEditor : ShaderConfigurationEditor
    {
        protected override ShaderConfigurationControl CreateEditorControl()
        {
            return new MaterialConfigurationControl();
        }

        class MaterialConfigurationControl : ShaderConfigurationControl
        {
            protected override IEnumerable<string> GetConfigurationNames()
            {
                return ShaderManager.LoadConfiguration().Shaders.Where(configuration => configuration is MaterialConfiguration)
                                                                .Select(configuration => configuration.Name);
            }
        }
    }
}
