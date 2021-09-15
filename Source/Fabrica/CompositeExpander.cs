// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using GEAviation.Fabrica.Model;
using System;
using System.Collections.Generic;

namespace GEAviation.Fabrica
{
    /// <summary>
    /// Used to duplicate and fill-in a part tree defined within a composite part.
    /// </summary>
    public class CompositeExpander : BlueprintVisitor
    {
        private IDictionary<string, IPartDefOrRef> mFeatureArguments;
        private IDictionary<string, IPropertyValueOrSlot> mPropertyArguments;

        /// <summary>
        /// A list of parts that were generated during the expansion process.
        /// </summary>
        public IList<IPartDefOrRef> AdditionalParts { get; private set; } = new List<IPartDefOrRef>();

        /// <summary>
        /// Overridden implementation of <see cref="visitIPartDefOrRef(ImmutableStack{object}, IPartDefOrRef)"/>.
        /// Used to swap <see cref="FeatureSlot"/> instances out with actual Feature values.
        /// </summary>
        public override IPartDefOrRef visitIPartDefOrRef(ImmutableStack<object> aContext, IPartDefOrRef aIPartDefOrRef)
        {
            IPartDefOrRef handlePart(Part aPart)
            {
                var lConcretePart = new Part(aPart);
                        
                lConcretePart.ID = Guid.NewGuid();

                AdditionalParts.Add(lConcretePart);

                return new IDPartRef() {  PartID = lConcretePart.ID };
            }

            IPartDefOrRef handlePartList(PartList aPartList)
            {
                var lNewList = new PartList();

                foreach(var lItem in aPartList)
                {
                    IPartDefOrRef lHandled = null;
                    if(lItem is Part lActualPart)
                    {
                        lHandled = handlePart(lActualPart);
                    }
                    else if(lItem is PartList lPartList)
                    {
                        lHandled = handlePartList(lPartList);
                    }
                    else if(lItem is PartDictionary lPartDict)
                    {
                        lHandled = handlePartDictionary(lPartDict);
                    }
                    else
                    {
                        lHandled = lItem.createCopy();
                    }

                    lNewList.Add(lHandled);
                }

                return lNewList;
            }

            IPartDefOrRef handlePartDictionary(PartDictionary aPartDictionary)
            {
                var lNewDictionary = new PartDictionary();

                foreach(var lItem in aPartDictionary)
                {
                    IPartDefOrRef lHandled = null;
                    if(lItem.Value is Part lActualPart)
                    {
                        lHandled = handlePart(lActualPart);
                    }
                    else if(lItem.Value is PartList lPartList)
                    {
                        lHandled = handlePartList(lPartList);
                    }
                    else if(lItem.Value is PartDictionary lPartDict)
                    {
                        lHandled = handlePartDictionary(lPartDict);
                    }
                    else
                    {
                        lHandled = lItem.Value.createCopy();
                    }

                    lNewDictionary[lItem.Key] = lHandled;
                }

                return lNewDictionary;
            }

            if(aIPartDefOrRef is FeatureSlot lFeatureSlot)
            { 
                if(mFeatureArguments.TryGetValue(lFeatureSlot.SlotName, out var lFeatureValue))
                {
                    IPartDefOrRef lFinalFeatureValue = null;

                    // Safety net in case the feature arguments contained "real" parts. Since the
                    // same slot can appear more than once in the composite, we have to make sure we
                    // don't duplicate a concrete each time the slot appears. Instead, we swap it for an ID Ref
                    // and use that.
                    if(lFeatureValue is Part lFullPart)
                    {
                        lFinalFeatureValue = handlePart(lFullPart);
                    }
                    else if(lFeatureValue is PartList lPartList)
                    {
                        lFinalFeatureValue = handlePartList(lPartList);
                    }
                    else if(lFeatureValue is PartDictionary lPartDict)
                    {
                        lFinalFeatureValue = handlePartDictionary(lPartDict);
                    }
                    else
                    {
                        lFinalFeatureValue = lFeatureValue.createCopy();
                    }

                    mFeatureArguments[lFeatureSlot.SlotName] = lFinalFeatureValue;
                    return lFinalFeatureValue;
                }
            }

            return base.visitIPartDefOrRef(aContext, aIPartDefOrRef);
        }

        /// <summary>
        /// Overridden implementation of <see cref="visitIPropertyValueOrSlot(ImmutableStack{object}, IPropertyValueOrSlot)"/>.
        /// Used to swap <see cref="PropertySlot"/> instances out with actual Property values.
        /// </summary>
        public override IPropertyValueOrSlot visitIPropertyValueOrSlot(ImmutableStack<object> aContext, IPropertyValueOrSlot aIPropertyValueOrSlot)
        {
            if(aIPropertyValueOrSlot is PropertySlot lPropertySlot)
            {
                if(mPropertyArguments.TryGetValue(lPropertySlot.SlotName, out var lPropertyValue))
                {
                    return lPropertyValue.createCopy();
                }
            }

            return base.visitIPropertyValueOrSlot(aContext, aIPropertyValueOrSlot);
        }

        /// <summary>
        /// Overridden implementation of <see cref="visitIPart(ImmutableStack{object}, IPart)"/>.
        /// Used to modify duplicated <see cref="Part"/> instances to give them new unique IDs and no names.
        /// </summary>
        public override IPart visitIPart(ImmutableStack<object> aContext, IPart aIPart)
        {
            aIPart.ID = Guid.NewGuid();
            aIPart.Name = String.Empty;
            return base.visitIPart(aContext, aIPart);
        }

        /// <summary>
        /// Class is only accessible via the static method.
        /// </summary>
        private CompositeExpander() { }

        /// <summary>
        /// Given a composite part, feature arguments and property arguments, this method
        /// expands and fills in a new instance of a composite part.
        /// </summary>
        /// <param name="aPartToExpand">
        /// The composite root part to expand.
        /// </param>
        /// <param name="aFeatureArguments">
        /// The feature arguments to make against feature slots found within the composite tree.
        /// </param>
        /// <param name="aPropertyArguments">
        /// The property arguments to make against property slots found within the composite tree.
        /// </param>
        /// <param name="aAdditionallyGeneratedParts">
        /// During expansion, additional parts outside of the part tree may be generated. This
        /// collection provides these additionally generated parts for use by the caller.
        /// </param>
        /// <returns>
        /// The expanded and filled in tree.
        /// </returns>
        public static Part expandPart(Part aPartToExpand, IDictionary<string, IPartDefOrRef> aFeatureArguments, IDictionary<string, IPropertyValueOrSlot> aPropertyArguments, out IEnumerable<IPartDefOrRef> aAdditionallyGeneratedParts )
        {
            var lExpander = new CompositeExpander();
            lExpander.mFeatureArguments = aFeatureArguments ?? throw new ArgumentNullException(nameof(aFeatureArguments));
            lExpander.mPropertyArguments = aPropertyArguments ?? throw new ArgumentNullException(nameof(aPropertyArguments));

            var lDuplicateTree = new Part(aPartToExpand);

            var lFinalPart = lExpander.visitPart(new ImmutableStack<object>("Root"), lDuplicateTree);
            aAdditionallyGeneratedParts = lExpander.AdditionalParts;

            return lFinalPart;
        }
    }
}
