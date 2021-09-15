// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace GEAviation.Fabrica.Model 
{
    public interface ICanHaveTemporaryID
    {
        /// <summary>
        /// When true, indicates that the Part's ID is a temporary ID and should
        /// not be persisted when saving Part data.
        /// </summary>
        bool HasTemporaryID { get; }
    }
}