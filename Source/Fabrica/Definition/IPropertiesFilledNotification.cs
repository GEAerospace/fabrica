// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GEAviation.Fabrica.Definition
{
    /// <summary>
    /// This interface can be used by Part implementations to be notified,
    /// after Part instantiation, that the properties marked with the
    /// <see cref="Definition.PropertyAttribute"/> have been set.
    /// </summary>
    public interface IPropertiesSetNotification
    {
        /// <summary>
        /// Called by Fabrica on behalf of the implementing Part type to
        /// notify the object that its properties have been filled and are
        /// ready to use.
        /// </summary>
        void propertiesSet();
    }
}
