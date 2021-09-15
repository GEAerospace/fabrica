// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace GEAviation.Fabrica.Definition 
{
    /// <summary>
    /// This interface represents the information necessary to identify and
    /// invoke a constructor for a Fabrica Part.
    /// </summary>
    public interface IPartConstructorInfo 
    {
        /// <summary>
        /// The constructor to call to use this PartConstructor. If null, no executable 
        /// code exists for the constructor.
        /// </summary>
        ConstructorInfo Constructor { get; }

        /// <summary>
        /// A collection of all of the Features (dependencies) of the Part Type that are
        /// intended to be loaded by Fabrica. This includes both 
        /// </summary>
        IDictionary<string, Type> Features { get; }

        /// <summary>
        /// A collection of all of the Required Features (dependencies) of the Part Type that are
        /// intended to be loaded by Fabrica. This will be either the same as, or a subset of, the
        /// <see cref="IPartConstructorInfo.Features"/> collection.
        /// </summary>
        IDictionary<string, Type> RequiredFeatures { get; }

        /// <summary>
        /// A collection of developer-provided descriptions for the Part's Features.
        /// The keys of this collection will be the same as the keys in the <see cref="IPartConstructorInfo.Features"/>
        /// collection, however this collection may only have a subset of those keys (or none at all).
        /// </summary>
        IDictionary<string, string> FeatureDescriptions { get; }

        /// <summary>
        /// The order that the features must appear in order to call the constructor.
        /// </summary>
        IList<string> FeatureOrder { get; }
    }
}