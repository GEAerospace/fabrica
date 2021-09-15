// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace GEAviation.Fabrica.Definition 
{
    /// <summary>
    /// <para>
    /// This interface can be used to further specify that a Part is also
    /// Part Locator, able to "find" parts given a particular Uri.
    /// </para>
    /// <para>
    /// Objects implementing this interface are not expected to search the
    /// existing Part pool, but rather provide access to a pool of objects/data
    /// not normally exposed via the Part system.
    /// </para>
    /// </summary>
    public interface IPartLocator
    {
        /// <summary>
        /// <para>
        /// When implemented by a class, this method will obtain and
        /// return a object for the specified <see cref="Uri"/>. 
        /// </para>
        /// <para>
        /// The relationship between the <see cref="Uri"/> and that object 
        /// is defined by the implementing class. A Uri must uniquely point
        /// to the same object so that, subsequent calls  for the same Uri
        /// return a functionally equivalent object.
        /// </para>
        /// <para>
        /// The part locator may also return null instead of an object. This
        /// indicates that no object for the specified Uri exists.
        /// </para>
        /// </summary>
        /// <param name="aPartUri">
        /// The <see cref="Uri"/> for the object being requested.
        /// </param>
        /// <returns>
        /// An object related to the Uri or null if no such object exists.
        /// </returns>
        object getPartFromUri( Uri aPartUri );
    }
}