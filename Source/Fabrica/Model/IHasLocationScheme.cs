// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace GEAviation.Fabrica.Model 
{
    /// <summary>
    /// This interface is for model elements that have a Uri scheme
    /// property.
    /// </summary>
    public interface IHasLocationScheme
    {
        /// <summary>
        /// The scheme of the item in the model.
        /// </summary>
        string LocationScheme { get; set; }
    }
}
