// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace GEAviation.Fabrica.Model
{
    /// <summary>
    /// This class will traverse a Fabrica Model and allows subclasses
    /// to override/hook "stops" along the way, much like Roslyn syntax 
    /// walkers.
    /// </summary>
    public class BlueprintVisitor
    {
        /// <summary>
        /// For a visitation handler, this enumeration specifies which side of
        /// the traversal the visit has occurred.
        /// </summary>
        public enum VisitOrder
        {
            /// <summary>
            /// The Prefix visit order indicates that the visit is occurring before any children
            /// are visited.
            /// </summary>
            Prefix,

            /// <summary>
            /// The Postfix visit order indicates that the visit is occurring after all children
            /// are visited.
            /// </summary>
            Postfix
        }

        /// <summary>
        /// Call this method to begin visiting a Fabrica BlueprintList and its child model objects.
        /// </summary>
        /// <param name="aBlueprintList">
        /// The BlueprintList to visit.
        /// </param>
        public IEnumerable<Blueprint> visitBlueprintList(IEnumerable<Blueprint> aBlueprintList)
        {
            return visitBlueprintList(new ImmutableStack<object>("Root"), aBlueprintList);
        }

        /// <summary>
        /// Call this method to begin visiting a Fabrica BlueprintList and its child model objects.
        /// </summary>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to the current object.
        /// </param>
        /// <param name="aBlueprintList">
        /// The BlueprintList to visit.
        /// </param>
        public virtual IEnumerable<Blueprint> visitBlueprintList(ImmutableStack<object> aContext, IEnumerable<Blueprint> aBlueprintList)
        {
            if(aBlueprintList == null) return null;

            OnVisitEverything(VisitOrder.Prefix, aContext, aBlueprintList);
            OnVisitBlueprintList(VisitOrder.Prefix, aContext, aBlueprintList);

            var lListContext = aContext.push(aBlueprintList);

            var lToReturn = aBlueprintList;
            var lNewList = new List<Blueprint>();

            foreach(var lBlueprint in aBlueprintList)
            {
                var lVisited = visitBlueprint(lListContext, lBlueprint);
                lNewList.Add(lVisited);
                if(lVisited != lBlueprint)
                {
                    lToReturn = lNewList;
                }
            }

            OnVisitEverything(VisitOrder.Postfix, aContext, lToReturn);
            OnVisitBlueprintList(VisitOrder.Postfix, aContext, lToReturn);

            return lToReturn;
        }

        /// <summary>
        /// Override this method to receive visitation for each BlueprintList.
        /// </summary>
        /// <param name="aOrder">
        /// Indicates if this visit is occurring before the object's children (<see cref="VisitOrder.Prefix"/>) or
        /// after the object's children (<see cref="VisitOrder.Postfix"/>).
        /// </param>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to this object.
        /// </param>
        /// <param name="aBlueprintList">
        /// The BlueprintList being visited.
        /// </param>
        protected virtual void OnVisitBlueprintList(VisitOrder aOrder, ImmutableStack<object> aContext, IEnumerable<Blueprint> aBlueprintList) { }

        /// <summary>
        /// Call this method to begin visiting a Fabrica Blueprint and its child model objects.
        /// </summary>
        /// <param name="aContext">
        /// Stack of parent blueprint objects to give context to the current object.
        /// </param>
        /// <param name="aBlueprint">
        /// The Blueprint to visit.
        /// </param>
        public Blueprint visitBlueprint(ImmutableStack<object> aContext, Blueprint aBlueprint)
        {
            if(aBlueprint == null) return null;

            OnVisitEverything(VisitOrder.Prefix, aContext, aBlueprint);
            OnVisitBlueprint(VisitOrder.Prefix, aContext, aBlueprint);

            var lNewContext = aContext.push(aBlueprint);
            var lToReturn = aBlueprint;
            var lNewBlueprint = new Blueprint(aBlueprint, true);

            foreach(var lAlias in aBlueprint.TypeAliases)
            {
                var lVisited = visitTypeAlias(lNewContext, lAlias.Value);
                lNewBlueprint.TypeAliases[lAlias.Key] = lVisited;
                if(lVisited != lAlias.Value)
                {
                    lToReturn = lNewBlueprint;
                }
            }

            foreach(var lComposite in aBlueprint.Composites)
            {
                var lVisited = visitCompositePartDef(lNewContext, lComposite.Value);
                lNewBlueprint.Composites[lComposite.Key] = lVisited;
                if(lVisited != lComposite.Value)
                {
                    lToReturn = lNewBlueprint;
                }
            }

            foreach(var lPart in aBlueprint.Parts)
            {
                var lVisited = visitIPart(lNewContext, lPart.Value);
                lNewBlueprint.Parts[lPart.Key] = lVisited;
                if(lVisited != lPart.Value)
                {
                    lToReturn = lNewBlueprint;
                }
            }

            OnVisitEverything(VisitOrder.Postfix, aContext, lToReturn);
            OnVisitBlueprint(VisitOrder.Postfix, aContext, lToReturn);

            return lToReturn;
        }

        /// <summary>
        /// Override this method to receive visitation for each Blueprint.
        /// </summary>
        /// <param name="aOrder">
        /// Indicates if this visit is occurring before the object's children (<see cref="VisitOrder.Prefix"/>) or
        /// after the object's children (<see cref="VisitOrder.Postfix"/>).
        /// </param>
        /// <param name="aContext">
        /// Stack of parent blueprint objects to give context to this object.
        /// </param>
        /// <param name="aBlueprint">
        /// The Blueprint being visited.
        /// </param>
        protected virtual void OnVisitBlueprint(VisitOrder aOrder, ImmutableStack<object> aContext, Blueprint aBlueprint) { }

        /// <summary>
        /// Call this method to begin visiting a Fabrica TypeAlias and its child model objects.
        /// </summary>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to the current object.
        /// </param>
        /// <param name="aTypeAlias">
        /// The TypeAlias to visit.
        /// </param>
        public virtual TypeAlias visitTypeAlias(ImmutableStack<object> aContext, TypeAlias aTypeAlias)
        {
            if(aTypeAlias == null) return null;

            OnVisitEverything(VisitOrder.Prefix, aContext, aTypeAlias);
            OnVisitTypeAlias(VisitOrder.Prefix, aContext, aTypeAlias);

            var lNewContext = aContext.push(aTypeAlias);
            var lToReturn = aTypeAlias;
            var lNewAlias = new TypeAlias(aTypeAlias, true);

            var lVisited = visitTypeDefinition(lNewContext, string.Empty, aTypeAlias.Type);
            lNewAlias.Type = lVisited;
            if(lVisited != aTypeAlias.Type)
            {
                lToReturn = lNewAlias;
            }

            OnVisitEverything(VisitOrder.Postfix, aContext, aTypeAlias);
            OnVisitTypeAlias(VisitOrder.Postfix, aContext, lToReturn);

            return lToReturn;
        }

        /// <summary>
        /// Override this method to receive visitation for each TypeAlias.
        /// </summary>
        /// <param name="aOrder">
        /// Indicates if this visit is occurring before the object's children (<see cref="VisitOrder.Prefix"/>) or
        /// after the object's children (<see cref="VisitOrder.Postfix"/>).
        /// </param>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to this object.
        /// </param>
        /// <param name="aTypeAlias">
        /// The TypeAlias being visited.
        /// </param>
        protected virtual void OnVisitTypeAlias(VisitOrder aOrder, ImmutableStack<object> aContext, TypeAlias aTypeAlias) { }

        /// <summary>
        /// Call this method to begin visiting a Fabrica TypeDefinition and its child model objects.
        /// </summary>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to the current object.
        /// </param>
        /// <param name="aTypeDefinition">
        /// The TypeDefinition to visit.
        /// </param>
        public virtual TypeDefinition visitTypeDefinition(ImmutableStack<object> aContext, string aTypeParamName, TypeDefinition aTypeDefinition)
        {
            if(aTypeDefinition == null) return null;
            
            OnVisitEverything(VisitOrder.Prefix, aContext, aTypeDefinition);
            OnVisitTypeDefinition(VisitOrder.Prefix, aContext, aTypeParamName, aTypeDefinition);

            var lNewContext = aContext.push(aTypeDefinition);
            var lToReturn = aTypeDefinition;
            var lNewTypeDef = new TypeDefinition(aTypeDefinition, true);
            
            foreach(var lTypeParam in aTypeDefinition.TypeParameters)
            {
                var lVisited = visitTypeDefinition(lNewContext, lTypeParam.Key, lTypeParam.Value);
                lNewTypeDef.TypeParameters[lTypeParam.Key] = lVisited;
                if(lVisited != lTypeParam.Value)
                {
                    lToReturn = lNewTypeDef;
                }
            }

            OnVisitEverything(VisitOrder.Postfix, aContext, lToReturn);
            OnVisitTypeDefinition(VisitOrder.Postfix, aContext, aTypeParamName, lToReturn);

            return lToReturn;
        }

        /// <summary>
        /// Override this method to receive visitation for each TypeDefinition.
        /// </summary>
        /// <param name="aOrder">
        /// Indicates if this visit is occurring before the object's children (<see cref="VisitOrder.Prefix"/>) or
        /// after the object's children (<see cref="VisitOrder.Postfix"/>).
        /// </param>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to this object.
        /// </param>
        /// <param name="aTypeDefinition">
        /// The TypeDefinition being visited.
        /// </param>
        protected virtual void OnVisitTypeDefinition(VisitOrder aOrder, ImmutableStack<object> aContext, string aTypeParamName, TypeDefinition aTypeDefinition) { }

        /// <summary>
        /// Call this method to begin visiting a Fabrica CompositePartDef and its child model objects.
        /// </summary>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to the current object.
        /// </param>
        /// <param name="aCompositePartDef">
        /// The CompositePartDef to visit.
        /// </param>
        public virtual CompositePartDef visitCompositePartDef(ImmutableStack<object> aContext, CompositePartDef aCompositePartDef)
        {
            if(aCompositePartDef == null) return null;

            OnVisitEverything(VisitOrder.Prefix, aContext, aCompositePartDef);
            OnVisitCompositePartDef(VisitOrder.Prefix, aContext, aCompositePartDef);

            var lNewContext = aContext.push(aCompositePartDef);

            var lToReturn = aCompositePartDef;
            var lNewCPD = new CompositePartDef(aCompositePartDef, true);

            var lVisited = visitPart(lNewContext, aCompositePartDef.RootPart);
            lNewCPD.RootPart = lVisited;

            if(lVisited != aCompositePartDef.RootPart)
            {
                lToReturn = lNewCPD;
            }

            OnVisitEverything(VisitOrder.Postfix, aContext, lToReturn);
            OnVisitCompositePartDef(VisitOrder.Postfix, aContext, lToReturn);

            return lToReturn;
        }

        /// <summary>
        /// Override this method to receive visitation for each CompositePartDef.
        /// </summary>
        /// <param name="aOrder">
        /// Indicates if this visit is occurring before the object's children (<see cref="VisitOrder.Prefix"/>) or
        /// after the object's children (<see cref="VisitOrder.Postfix"/>).
        /// </param>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to this object.
        /// </param>
        /// <param name="aCompositePartDef">
        /// The CompositePartDef being visited.
        /// </param>
        protected virtual void OnVisitCompositePartDef(VisitOrder aOrder, ImmutableStack<object> aContext, CompositePartDef aCompositePartDef) { }

        /// <summary>
        /// Call this method to begin visiting a Fabrica IPart and its child model objects.
        /// </summary>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to the current object.
        /// </param>
        /// <param name="aIPart">
        /// The IPart to visit.
        /// </param>
        public virtual IPart visitIPart(ImmutableStack<object> aContext, IPart aIPart)
        {
            if(aIPart == null) return null;

            // Intentionally skipping the visit pattern here as this is just meant to
            // disambiguate the type of IPart.
            switch(aIPart)
            {
                case Part lPart:
                    return visitPart(aContext, lPart);

                case ExternalPart lExternalPart:
                    return visitExternalPart(aContext, lExternalPart);

                case UndefinedPart lUndefinedPart:
                    return visitUndefinedPart(aContext, lUndefinedPart);

                case PartList lPartList:
                    return visitPartList(aContext, lPartList);

                case PartDictionary lPartDictionary:
                    return visitPartDictionary(aContext, lPartDictionary);
            }

            return null;
        }

        /// <summary>
        /// Call this method to begin visiting a Fabrica IPartDefOrRef and its child model objects.
        /// </summary>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to the current object.
        /// </param>
        /// <param name="aIPartDefOrRef">
        /// The IPartDefOrRef to visit.
        /// </param>
        public virtual IPartDefOrRef visitIPartDefOrRef(ImmutableStack<object> aContext, IPartDefOrRef aIPartDefOrRef)
        {
            if(aIPartDefOrRef == null) return null;

            switch(aIPartDefOrRef)
            {
                case NamedPartRef lNameRef:
                    return visitNamedPartRef(aContext, lNameRef);

                case UriPartRef lUriRef:
                    return visitUriPartRef(aContext, lUriRef);

                case IDPartRef lIdRef:
                    return visitIDPartRef(aContext, lIdRef);

                case ConstantValue lConstant:
                    return visitConstantValue(aContext, lConstant);

                case FeatureSlot lFeatureSlot:
                    return visitFeatureSlot(aContext, lFeatureSlot);

                case CompositePartDef lCompositeRef:
                    return visitCompositePartDef(aContext, lCompositeRef);

                case IPart lIPart:
                    return visitIPart(aContext, lIPart);
            }

            return null;
        }

        /// <summary>
        /// Override this method to receive visitation for each IPartDefOrRef.
        /// </summary>
        /// <param name="aOrder">
        /// Indicates if this visit is occurring before the object's children (<see cref="VisitOrder.Prefix"/>) or
        /// after the object's children (<see cref="VisitOrder.Postfix"/>).
        /// </param>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to this object.
        /// </param>
        /// <param name="aIPartDefOrRef">
        /// The IPartDefOrRef being visited.
        /// </param>
        protected virtual void OnVisitIPartDefOrRef(VisitOrder aOrder, ImmutableStack<object> aContext, IPartDefOrRef aIPartDefOrRef) { }

        /// <summary>
        /// Call this method to begin visiting a Fabrica Part and its child model objects.
        /// </summary>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to the current object.
        /// </param>
        /// <param name="aPart">
        /// The Part to visit.
        /// </param>
        public virtual Part visitPart(ImmutableStack<object> aContext, Part aPart)
        {
            if(aPart == null) return null;

            OnVisitEverything(VisitOrder.Prefix, aContext, aPart);
            OnVisitPart(VisitOrder.Prefix, aContext, aPart);

            var lNewContext = aContext.push(aPart);

            Part lToReturn = aPart;
            Part lNewPart = new Part(aPart, true);

            var lVisitedTypeDefOrRef = visitITypeDefOrRef(lNewContext, aPart.RuntimeType);
            
            lNewPart.RuntimeType = lVisitedTypeDefOrRef;

            if(lVisitedTypeDefOrRef != aPart.RuntimeType)
            {
                lToReturn = lNewPart;
            }

            foreach(var lFeature in aPart.Features)
            {
                var lVisited = visitFeature(lNewContext, lFeature);
                lNewPart.Features[lVisited.Key] = lVisited.Value;
                if(lVisited.Value != lFeature.Value || lVisited.Key != lFeature.Key)
                {
                    lToReturn = lNewPart;
                }                
            }

            foreach(var lProperty in aPart.Properties)
            {
                var lVisited = visitProperty(lNewContext, lProperty);
                lNewPart.Properties[lVisited.Key] = lVisited.Value;
                if(lVisited.Value != lProperty.Value || lVisited.Key != lProperty.Key)
                {
                    lToReturn = lNewPart;
                }
            }

            OnVisitEverything(VisitOrder.Postfix, aContext, lToReturn);
            OnVisitPart(VisitOrder.Postfix, aContext, lToReturn);

            return lToReturn;
        }

        /// <summary>
        /// Override this method to receive visitation for each Part.
        /// </summary>
        /// <param name="aOrder">
        /// Indicates if this visit is occurring before the object's children (<see cref="VisitOrder.Prefix"/>) or
        /// after the object's children (<see cref="VisitOrder.Postfix"/>).
        /// </param>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to this object.
        /// </param>
        /// <param name="aPart">
        /// The Part being visited.
        /// </param>
        protected virtual void OnVisitPart(VisitOrder aOrder, ImmutableStack<object> aContext, Part aPart) { }

        /// <summary>
        /// Call this method to begin visiting a Fabrica ITypeDefOrRef and its child model objects.
        /// </summary>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to the current object.
        /// </param>
        /// <param name="aITypeDefOrRef">
        /// The ITypeDefOrRef to visit.
        /// </param>
        public virtual ITypeDefOrRef visitITypeDefOrRef(ImmutableStack<object> aContext, ITypeDefOrRef aITypeDefOrRef)
        {
            if(aITypeDefOrRef == null) return null;

            switch(aITypeDefOrRef)
            {
                case TypeAlias lAlias:
                    return visitTypeAlias(aContext, lAlias);

                case TypeDefinition lDef:
                    return visitTypeDefinition(aContext, string.Empty, lDef);

                case CompositeTypeRef lCompositeRef:
                    return visitCompositeTypeRef(aContext, lCompositeRef);
            }

            return null;
        }

        /// <summary>
        /// Call this method to begin visiting a Fabrica CompositeTypeRef and its child model objects.
        /// </summary>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to the current object.
        /// </param>
        /// <param name="aCompositeTypeRef">
        /// The CompositeTypeRef to visit.
        /// </param>
        public virtual CompositeTypeRef visitCompositeTypeRef(ImmutableStack<object> aContext, CompositeTypeRef aCompositeTypeRef)
        {
            if(aCompositeTypeRef == null) return null;

            OnVisitEverything(VisitOrder.Prefix, aContext, aCompositeTypeRef);
            OnVisitCompositeTypeRef(VisitOrder.Prefix, aContext, aCompositeTypeRef);
            OnVisitEverything(VisitOrder.Postfix, aContext, aCompositeTypeRef);
            OnVisitCompositeTypeRef(VisitOrder.Postfix, aContext, aCompositeTypeRef);

            return aCompositeTypeRef;
        }

        /// <summary>
        /// Override this method to receive visitation for each CompositeTypeRef.
        /// </summary>
        /// <param name="aOrder">
        /// Indicates if this visit is occurring before the object's children (<see cref="VisitOrder.Prefix"/>) or
        /// after the object's children (<see cref="VisitOrder.Postfix"/>).
        /// </param>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to this object.
        /// </param>
        /// <param name="aCompositeTypeRef">
        /// The CompositeTypeRef being visited.
        /// </param>
        protected virtual void OnVisitCompositeTypeRef(VisitOrder aOrder, ImmutableStack<object> aContext, CompositeTypeRef aCompositeTypeRef) { }

        /// <summary>
        /// Call this method to begin visiting a Fabrica Property and its child model objects.
        /// </summary>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to the current object.
        /// </param>
        /// <param name="aProperty">
        /// The Property to visit.
        /// </param>
        public virtual KeyValuePair<string, IPropertyValueOrSlot> visitProperty(ImmutableStack<object> aContext, KeyValuePair<string, IPropertyValueOrSlot> aProperty)
        {
            OnVisitEverything(VisitOrder.Prefix, aContext, aProperty);
            OnVisitProperty(VisitOrder.Prefix, aContext, aProperty);

            var lNewContext = aContext.push(aProperty);
            var lNewKV = aProperty;

            var lVisited = visitIPropertyValueOrSlot(lNewContext, aProperty.Value);

            if(lVisited != aProperty.Value)
            {
                lNewKV = new KeyValuePair<string, IPropertyValueOrSlot>(aProperty.Key, lVisited);
            }

            OnVisitEverything(VisitOrder.Postfix, aContext, lNewKV);
            OnVisitProperty(VisitOrder.Postfix, aContext, lNewKV);

            return lNewKV;
        }

        /// <summary>
        /// Override this method to receive visitation for each Property.
        /// </summary>
        /// <param name="aOrder">
        /// Indicates if this visit is occurring before the object's children (<see cref="VisitOrder.Prefix"/>) or
        /// after the object's children (<see cref="VisitOrder.Postfix"/>).
        /// </param>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to this object.
        /// </param>
        /// <param name="aProperty">
        /// The Property being visited.
        /// </param>
        protected virtual void OnVisitProperty(VisitOrder aOrder, ImmutableStack<object> aContext, KeyValuePair<string, IPropertyValueOrSlot> aProperty) { }

        /// <summary>
        /// Call this method to begin visiting a Fabrica IPropertyValueOrSlot and its child model objects.
        /// </summary>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to the current object.
        /// </param>
        /// <param name="aIPropertyValueOrSlot">
        /// The IPropertyValueOrSlot to visit.
        /// </param>
        public virtual IPropertyValueOrSlot visitIPropertyValueOrSlot(ImmutableStack<object> aContext, IPropertyValueOrSlot aIPropertyValueOrSlot)
        {
            if(aIPropertyValueOrSlot == null) return null;

            switch(aIPropertyValueOrSlot)
            {
                case PropertyValue lPropValue:
                    return visitPropertyValue(aContext, lPropValue);

                case PropertySlot lPropSlot:
                    return visitPropertySlot(aContext, lPropSlot);
            }

            return null;
        }

        /// <summary>
        /// Call this method to begin visiting a Fabrica PropertyValue and its child model objects.
        /// </summary>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to the current object.
        /// </param>
        /// <param name="aPropertyValue">
        /// The PropertyValue to visit.
        /// </param>
        public virtual PropertyValue visitPropertyValue(ImmutableStack<object> aContext, PropertyValue aPropertyValue)
        {
            if(aPropertyValue == null) return null;

            OnVisitEverything(VisitOrder.Prefix, aContext, aPropertyValue);
            OnVisitPropertyValue(VisitOrder.Prefix, aContext, aPropertyValue);
            OnVisitEverything(VisitOrder.Postfix, aContext, aPropertyValue);
            OnVisitPropertyValue(VisitOrder.Postfix, aContext, aPropertyValue);

            return aPropertyValue;
        }

        /// <summary>
        /// Override this method to receive visitation for each PropertyValue.
        /// </summary>
        /// <param name="aOrder">
        /// Indicates if this visit is occurring before the object's children (<see cref="VisitOrder.Prefix"/>) or
        /// after the object's children (<see cref="VisitOrder.Postfix"/>).
        /// </param>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to this object.
        /// </param>
        /// <param name="aPropertyValue">
        /// The PropertyValue being visited.
        /// </param>
        protected virtual void OnVisitPropertyValue(VisitOrder aOrder, ImmutableStack<object> aContext, PropertyValue aPropertyValue) { }

        /// <summary>
        /// Call this method to begin visiting a Fabrica PropertySlot and its child model objects.
        /// </summary>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to the current object.
        /// </param>
        /// <param name="aPropertySlot">
        /// The PropertySlot to visit.
        /// </param>
        public virtual PropertySlot visitPropertySlot(ImmutableStack<object> aContext, PropertySlot aPropertySlot)
        {
            if(aPropertySlot == null) return null;

            OnVisitEverything(VisitOrder.Prefix, aContext, aPropertySlot);
            OnVisitPropertySlot(VisitOrder.Prefix, aContext, aPropertySlot);
            OnVisitEverything(VisitOrder.Postfix, aContext, aPropertySlot);
            OnVisitPropertySlot(VisitOrder.Postfix, aContext, aPropertySlot);

            return aPropertySlot;
        }

        /// <summary>
        /// Override this method to receive visitation for each PropertySlot.
        /// </summary>
        /// <param name="aOrder">
        /// Indicates if this visit is occurring before the object's children (<see cref="VisitOrder.Prefix"/>) or
        /// after the object's children (<see cref="VisitOrder.Postfix"/>).
        /// </param>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to this object.
        /// </param>
        /// <param name="aPropertySlot">
        /// The PropertySlot being visited.
        /// </param>
        protected virtual void OnVisitPropertySlot(VisitOrder aOrder, ImmutableStack<object> aContext, PropertySlot aPropertySlot) { }

        /// <summary>
        /// Call this method to begin visiting a Fabrica Feature and its child model objects.
        /// </summary>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to the current object.
        /// </param>
        /// <param name="aFeature">
        /// The Feature to visit.
        /// </param>
        public virtual KeyValuePair<string, IPartDefOrRef> visitFeature(ImmutableStack<object> aContext, KeyValuePair<string, IPartDefOrRef> aFeature)
        {
            OnVisitEverything(VisitOrder.Prefix, aContext, aFeature);
            OnVisitFeature(VisitOrder.Prefix, aContext, aFeature);

            var lNewContext = aContext.push(aFeature);

            var lNewKV = aFeature;

            IPartDefOrRef lVisited = visitIPartDefOrRef(lNewContext, aFeature.Value);

            if(lVisited != aFeature.Value)
            {
                lNewKV = new KeyValuePair<string, IPartDefOrRef>(aFeature.Key, lVisited);
            }

            OnVisitEverything(VisitOrder.Postfix, aContext, lNewKV);
            OnVisitFeature(VisitOrder.Postfix, aContext, lNewKV);

            return lNewKV;
        }

        /// <summary>
        /// Override this method to receive visitation for each Feature.
        /// </summary>
        /// <param name="aOrder">
        /// Indicates if this visit is occurring before the object's children (<see cref="VisitOrder.Prefix"/>) or
        /// after the object's children (<see cref="VisitOrder.Postfix"/>).
        /// </param>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to this object.
        /// </param>
        /// <param name="aFeature">
        /// The Feature being visited.
        /// </param>
        protected virtual void OnVisitFeature(VisitOrder aOrder, ImmutableStack<object> aContext, KeyValuePair<string, IPartDefOrRef> aFeature) { }

        /// <summary>
        /// Call this method to begin visiting a Fabrica ExternalPart and its child model objects.
        /// </summary>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to the current object.
        /// </param>
        /// <param name="aExternalPart">
        /// The ExternalPart to visit.
        /// </param>
        public virtual ExternalPart visitExternalPart(ImmutableStack<object> aContext, ExternalPart aExternalPart)
        {
            if(aExternalPart == null) return null;

            OnVisitEverything(VisitOrder.Prefix, aContext, aExternalPart);
            OnVisitExternalPart(VisitOrder.Prefix, aContext, aExternalPart);
            OnVisitEverything(VisitOrder.Postfix, aContext, aExternalPart);
            OnVisitExternalPart(VisitOrder.Postfix, aContext, aExternalPart);

            return aExternalPart;
        }

        /// <summary>
        /// Override this method to receive visitation for each ExternalPart.
        /// </summary>
        /// <param name="aOrder">
        /// Indicates if this visit is occurring before the object's children (<see cref="VisitOrder.Prefix"/>) or
        /// after the object's children (<see cref="VisitOrder.Postfix"/>).
        /// </param>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to this object.
        /// </param>
        /// <param name="aExternalPart">
        /// The ExternalPart being visited.
        /// </param>
        protected virtual void OnVisitExternalPart(VisitOrder aOrder, ImmutableStack<object> aContext, ExternalPart aExternalPart) { }

        /// <summary>
        /// Call this method to begin visiting a Fabrica UndefinedPart and its child model objects.
        /// </summary>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to the current object.
        /// </param>
        /// <param name="aUndefinedPart">
        /// The UndefinedPart to visit.
        /// </param>
        public virtual UndefinedPart visitUndefinedPart(ImmutableStack<object> aContext, UndefinedPart aUndefinedPart)
        {
            if(aUndefinedPart == null) return null;

            OnVisitEverything(VisitOrder.Prefix, aContext, aUndefinedPart);
            OnVisitUndefinedPart(VisitOrder.Prefix, aContext, aUndefinedPart);
            OnVisitEverything(VisitOrder.Postfix, aContext, aUndefinedPart);
            OnVisitUndefinedPart(VisitOrder.Postfix, aContext, aUndefinedPart);

            return aUndefinedPart;
        }

        /// <summary>
        /// Override this method to receive visitation for each UndefinedPart.
        /// </summary>
        /// <param name="aOrder">
        /// Indicates if this visit is occurring before the object's children (<see cref="VisitOrder.Prefix"/>) or
        /// after the object's children (<see cref="VisitOrder.Postfix"/>).
        /// </param>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to this object.
        /// </param>
        /// <param name="aUndefinedPart">
        /// The UndefinedPart being visited.
        /// </param>
        protected virtual void OnVisitUndefinedPart(VisitOrder aOrder, ImmutableStack<object> aContext, UndefinedPart aUndefinedPart) { }

        /// <summary>
        /// Call this method to begin visiting a Fabrica PartList and its child model objects.
        /// </summary>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to the current object.
        /// </param>
        /// <param name="aPartList">
        /// The PartList to visit.
        /// </param>
        public virtual PartList visitPartList(ImmutableStack<object> aContext, PartList aPartList)
        {
            if(aPartList == null) return null;

            OnVisitEverything(VisitOrder.Prefix, aContext, aPartList);
            OnVisitPartList(VisitOrder.Prefix, aContext, aPartList);

            var lNewContext = aContext.push(aPartList);

            PartList lToReturn = aPartList;
            PartList lNewPartList = new PartList(aPartList, true);

            for (int lIndex = 0; lIndex < aPartList.Count; lIndex++)
            {
                IPartDefOrRef lPart = aPartList[lIndex];
                var lNewItem = visitPartListItem(lNewContext, new Tuple<int, IPartDefOrRef>(lIndex, lPart));
                var lVisited = lNewItem.Item2;
                lNewPartList.Add(lVisited);
                if(lVisited != lPart)
                {
                    lToReturn = lNewPartList;
                }
            }

            OnVisitEverything(VisitOrder.Postfix, aContext, lToReturn);
            OnVisitPartList(VisitOrder.Postfix, aContext, lToReturn);

            return lToReturn;
        }

        /// <summary>
        /// Override this method to receive visitation for each PartList.
        /// </summary>
        /// <param name="aOrder">
        /// Indicates if this visit is occurring before the object's children (<see cref="VisitOrder.Prefix"/>) or
        /// after the object's children (<see cref="VisitOrder.Postfix"/>).
        /// </param>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to this object.
        /// </param>
        /// <param name="aPartList">
        /// The PartList being visited.
        /// </param>
        protected virtual void OnVisitPartList(VisitOrder aOrder, ImmutableStack<object> aContext, PartList aPartList) { }
        
        /// <summary>
        /// Call this method to begin visiting a PartList Item.
        /// </summary>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to the current object.
        /// </param>
        /// <param name="aPartList">
        /// The PartList Item to visit.
        /// </param>
        public virtual Tuple<int, IPartDefOrRef> visitPartListItem(ImmutableStack<object> aContext, Tuple<int, IPartDefOrRef> aItem)
        {
            if(aItem.Item2 == null) return null;

            OnVisitEverything(VisitOrder.Prefix, aContext, aItem);
            OnVisitPartListItem(VisitOrder.Prefix, aContext, aItem);

            var lNewContext = aContext.push(aItem);
            var lToReturn = aItem;
            var lVisited = visitIPartDefOrRef(lNewContext, aItem.Item2);

            if(lVisited != aItem.Item2)
            {
                lToReturn = new Tuple<int, IPartDefOrRef>(aItem.Item1, lVisited);
            }

            OnVisitEverything(VisitOrder.Postfix, aContext, lToReturn);
            OnVisitPartListItem(VisitOrder.Postfix, aContext, lToReturn);

            return lToReturn;
        }

        /// <summary>
        /// Override this method to receive visitation for each PartList Item.
        /// </summary>
        /// <param name="aOrder">
        /// Indicates if this visit is occurring before the object's children (<see cref="VisitOrder.Prefix"/>) or
        /// after the object's children (<see cref="VisitOrder.Postfix"/>).
        /// </param>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to this object.
        /// </param>
        /// <param name="aPartList">
        /// The PartList Item being visited.
        /// </param>
        protected virtual void OnVisitPartListItem(VisitOrder aOrder, ImmutableStack<object> aContext, Tuple<int, IPartDefOrRef> aPartList) { }

        /// <summary>
        /// Call this method to begin visiting a Fabrica PartDictionary and its child model objects.
        /// </summary>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to the current object.
        /// </param>
        /// <param name="aPartDictionary">
        /// The PartDictionary to visit.
        /// </param>
        public virtual PartDictionary visitPartDictionary(ImmutableStack<object> aContext, PartDictionary aPartDictionary)
        {
            if(aPartDictionary == null) return null;

            OnVisitEverything(VisitOrder.Prefix, aContext, aPartDictionary);
            OnVisitPartDictionary(VisitOrder.Prefix, aContext, aPartDictionary);

            var lNewContext = aContext.push(aPartDictionary);
            
            PartDictionary lToReturn = aPartDictionary;
            PartDictionary lNewDictionary = new PartDictionary(aPartDictionary, true);
            
            foreach(var lPair in aPartDictionary)
            {
                KeyValuePair<string, IPartDefOrRef> lVisited = visitPartDictionaryItem(lNewContext, lPair);
                if(lPair.Value != lVisited.Value)
                {
                    lToReturn = lNewDictionary;
                }
                lNewDictionary[lVisited.Key] = lVisited.Value;
            }

            OnVisitEverything(VisitOrder.Postfix, aContext, lToReturn);
            OnVisitPartDictionary(VisitOrder.Postfix, aContext, lToReturn);

            return lToReturn;
        }

        /// <summary>
        /// Override this method to receive visitation for each PartDictionary.
        /// </summary>
        /// <param name="aOrder">
        /// Indicates if this visit is occurring before the object's children (<see cref="VisitOrder.Prefix"/>) or
        /// after the object's children (<see cref="VisitOrder.Postfix"/>).
        /// </param>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to this object.
        /// </param>
        /// <param name="aPartDictionary">
        /// The PartDictionary being visited.
        /// </param>
        protected virtual void OnVisitPartDictionary(VisitOrder aOrder, ImmutableStack<object> aContext, PartDictionary aPartDictionary) { }

        /// <summary>
        /// Call this method to begin visiting a Fabrica PartDictionary and its child model objects.
        /// </summary>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to the current object.
        /// </param>
        /// <param name="aPartDictionary">
        /// The PartDictionary to visit.
        /// </param>
        public virtual KeyValuePair<string, IPartDefOrRef> visitPartDictionaryItem(ImmutableStack<object> aContext, KeyValuePair<string, IPartDefOrRef> aPartDictionaryItem)
        {
            OnVisitEverything(VisitOrder.Prefix, aContext, aPartDictionaryItem);
            OnVisitPartDictionaryItem(VisitOrder.Prefix, aContext, aPartDictionaryItem);

            var lNewContext = aContext.push(aPartDictionaryItem);

            IPartDefOrRef lVisited = visitIPartDefOrRef(lNewContext, aPartDictionaryItem.Value);

            var lToReturn = aPartDictionaryItem;
            if(lVisited != aPartDictionaryItem.Value)
            {
                lToReturn = new KeyValuePair<string, IPartDefOrRef>(lToReturn.Key, lVisited);
            }

            OnVisitEverything(VisitOrder.Postfix, aContext, lToReturn);
            OnVisitPartDictionaryItem(VisitOrder.Postfix, aContext, lToReturn);

            return lToReturn;
        }

        /// <summary>
        /// Override this method to receive visitation for each PartDictionary Item.
        /// </summary>
        /// <param name="aOrder">
        /// Indicates if this visit is occurring before the object's children (<see cref="VisitOrder.Prefix"/>) or
        /// after the object's children (<see cref="VisitOrder.Postfix"/>).
        /// </param>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to this object.
        /// </param>
        /// <param name="aPartDictionary">
        /// The PartDictionary being visited.
        /// </param>
        protected virtual void OnVisitPartDictionaryItem(VisitOrder aOrder, ImmutableStack<object> aContext, KeyValuePair<string, IPartDefOrRef> aPartDictionaryItem) { }

        /// <summary>
        /// Call this method to begin visiting a Fabrica NamedPartRef and its child model objects.
        /// </summary>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to the current object.
        /// </param>
        /// <param name="aNamedPartRef">
        /// The NamedPartRef to visit.
        /// </param>
        public virtual NamedPartRef visitNamedPartRef(ImmutableStack<object> aContext, NamedPartRef aNamedPartRef)
        {
            if(aNamedPartRef == null) return null;

            OnVisitEverything(VisitOrder.Prefix, aContext, aNamedPartRef);
            OnVisitNamedPartRef(VisitOrder.Prefix, aContext, aNamedPartRef);
            OnVisitEverything(VisitOrder.Postfix, aContext, aNamedPartRef);
            OnVisitNamedPartRef(VisitOrder.Postfix, aContext, aNamedPartRef);

            return aNamedPartRef;
        }

        /// <summary>
        /// Override this method to receive visitation for each NamedPartRef.
        /// </summary>
        /// <param name="aOrder">
        /// Indicates if this visit is occurring before the object's children (<see cref="VisitOrder.Prefix"/>) or
        /// after the object's children (<see cref="VisitOrder.Postfix"/>).
        /// </param>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to this object.
        /// </param>
        /// <param name="aNamedPartRef">
        /// The NamedPartRef being visited.
        /// </param>
        protected virtual void OnVisitNamedPartRef(VisitOrder aOrder, ImmutableStack<object> aContext, NamedPartRef aNamedPartRef) { }

        /// <summary>
        /// Call this method to begin visiting a Fabrica IDPartRef and its child model objects.
        /// </summary>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to the current object.
        /// </param>
        /// <param name="aIDPartRef">
        /// The IDPartRef to visit.
        /// </param>
        public virtual IDPartRef visitIDPartRef(ImmutableStack<object> aContext, IDPartRef aIDPartRef)
        {
            if(aIDPartRef == null) return null;

            OnVisitEverything(VisitOrder.Prefix, aContext, aIDPartRef);
            OnVisitIDPartRef(VisitOrder.Prefix, aContext, aIDPartRef);
            OnVisitEverything(VisitOrder.Postfix, aContext, aIDPartRef);
            OnVisitIDPartRef(VisitOrder.Postfix, aContext, aIDPartRef);

            return aIDPartRef;
        }

        /// <summary>
        /// Override this method to receive visitation for each IDPartRef.
        /// </summary>
        /// <param name="aOrder">
        /// Indicates if this visit is occurring before the object's children (<see cref="VisitOrder.Prefix"/>) or
        /// after the object's children (<see cref="VisitOrder.Postfix"/>).
        /// </param>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to this object.
        /// </param>
        /// <param name="aIDPartRef">
        /// The IDPartRef being visited.
        /// </param>
        protected virtual void OnVisitIDPartRef(VisitOrder aOrder, ImmutableStack<object> aContext, IDPartRef aIDPartRef) { }

        /// <summary>
        /// Call this method to begin visiting a Fabrica UriPartRef and its child model objects.
        /// </summary>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to the current object.
        /// </param>
        /// <param name="aUriPartRef">
        /// The UriPartRef to visit.
        /// </param>
        public virtual UriPartRef visitUriPartRef(ImmutableStack<object> aContext, UriPartRef aUriPartRef)
        {
            if(aUriPartRef == null) return null;

            OnVisitEverything(VisitOrder.Prefix, aContext, aUriPartRef);
            OnVisitUriPartRef(VisitOrder.Prefix, aContext, aUriPartRef);
            OnVisitEverything(VisitOrder.Postfix, aContext, aUriPartRef);
            OnVisitUriPartRef(VisitOrder.Postfix, aContext, aUriPartRef);

            return aUriPartRef;
        }

        /// <summary>
        /// Override this method to receive visitation for each UriPartRef.
        /// </summary>
        /// <param name="aOrder">
        /// Indicates if this visit is occurring before the object's children (<see cref="VisitOrder.Prefix"/>) or
        /// after the object's children (<see cref="VisitOrder.Postfix"/>).
        /// </param>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to this object.
        /// </param>
        /// <param name="aUriPartRef">
        /// The UriPartRef being visited.
        /// </param>
        protected virtual void OnVisitUriPartRef(VisitOrder aOrder, ImmutableStack<object> aContext, UriPartRef aUriPartRef) { }

        /// <summary>
        /// Call this method to begin visiting a Fabrica ConstantValue and its child model objects.
        /// </summary>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to the current object.
        /// </param>
        /// <param name="aConstantValue">
        /// The ConstantValue to visit.
        /// </param>
        public virtual ConstantValue visitConstantValue(ImmutableStack<object> aContext, ConstantValue aConstantValue)
        {
            if(aConstantValue == null) return null;

            OnVisitEverything(VisitOrder.Prefix, aContext, aConstantValue);
            OnVisitConstantValue(VisitOrder.Prefix, aContext, aConstantValue);
            OnVisitEverything(VisitOrder.Postfix, aContext, aConstantValue);
            OnVisitConstantValue(VisitOrder.Postfix, aContext, aConstantValue);

            return aConstantValue;
        }

        /// <summary>
        /// Override this method to receive visitation for each ConstantValue.
        /// </summary>
        /// <param name="aOrder">
        /// Indicates if this visit is occurring before the object's children (<see cref="VisitOrder.Prefix"/>) or
        /// after the object's children (<see cref="VisitOrder.Postfix"/>).
        /// </param>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to this object.
        /// </param>
        /// <param name="aConstantValue">
        /// The ConstantValue being visited.
        /// </param>
        protected virtual void OnVisitConstantValue(VisitOrder aOrder, ImmutableStack<object> aContext, ConstantValue aConstantValue) { }

        /// <summary>
        /// Call this method to begin visiting a Fabrica FeatureSlot and its child model objects.
        /// </summary>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to the current object.
        /// </param>
        /// <param name="aFeatureSlot">
        /// The FeatureSlot to visit.
        /// </param>
        public virtual FeatureSlot visitFeatureSlot(ImmutableStack<object> aContext, FeatureSlot aFeatureSlot)
        {
            if(aFeatureSlot == null) return null;

            OnVisitEverything(VisitOrder.Prefix, aContext, aFeatureSlot);
            OnVisitFeatureSlot(VisitOrder.Prefix, aContext, aFeatureSlot);
            OnVisitEverything(VisitOrder.Postfix, aContext, aFeatureSlot);
            OnVisitFeatureSlot(VisitOrder.Postfix, aContext, aFeatureSlot);

            return aFeatureSlot;
        }

        /// <summary>
        /// Override this method to receive visitation for each FeatureSlot.
        /// </summary>
        /// <param name="aOrder">
        /// Indicates if this visit is occurring before the object's children (<see cref="VisitOrder.Prefix"/>) or
        /// after the object's children (<see cref="VisitOrder.Postfix"/>).
        /// </param>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to this object.
        /// </param>
        /// <param name="aFeatureSlot">
        /// The FeatureSlot being visited.
        /// </param>
        protected virtual void OnVisitFeatureSlot(VisitOrder aOrder, ImmutableStack<object> aContext, FeatureSlot aFeatureSlot) { }

        /// <summary>
        /// Override this method to receive visitation for everything.
        /// </summary>
        /// <param name="aOrder">
        /// Indicates if this visit is occurring before the object's children (<see cref="VisitOrder.Prefix"/>) or
        /// after the object's children (<see cref="VisitOrder.Postfix"/>).
        /// </param>
        /// <param name="aContext">
        /// Stack of parent fabrica objects to give context to this object.
        /// </param>
        /// <param name="aVisitedObject">
        /// The fabrica object being visited.
        /// </param>
        protected virtual void OnVisitEverything(VisitOrder aOrder, ImmutableStack<object> aContext, object aVisitedObject) { }
    }
}
