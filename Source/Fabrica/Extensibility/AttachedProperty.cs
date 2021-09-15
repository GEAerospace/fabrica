// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

namespace GEAviation.Fabrica.Extensibility
{
    /// <summary>
    /// This class leverages <see cref="ConditionalWeakTable{TKey,TValue}"/> to allow
    /// the attachement of additional data to a <see cref="object"/> without having to
    /// modify the object itself. This works a little like XAML/WPF's Attached Property
    /// system.
    /// </summary>
    /// <remarks>
    /// Instances of this class represent the attached property globally throughout the
    /// current <see cref="AppDomain"/>. A common use may be to create instances of this
    /// class statically on a globaly accessible static class or singleton, and use those 
    /// <see cref="AttachedProperty{PropertyType}"/> instances throughout the application.
    /// </remarks>
    /// <typeparam name="PropertyType">
    /// The type of data the property will hold.
    /// </typeparam>
    public sealed class AttachedProperty<PropertyType>
    {
        /// <summary>
        /// This nested class is used to box values into a reference type so that
        /// they can be used as values in the <see cref="ConditionalWeakTable{TKey, TValue}"/>.
        /// </summary>
        /// <typeparam name="BoxingType">
        /// The data type being boxed.
        /// </typeparam>
        private class ValueBox<BoxingType>
        {
            public BoxingType Value { get; set; }

            public static implicit operator BoxingType(ValueBox<BoxingType> aBox)
            {
                return aBox.Value;
            }

            public static implicit operator ValueBox<BoxingType>(BoxingType aBoxingValue)
            {
                return new ValueBox<BoxingType>() { Value = aBoxingValue };
            }
        }

        private ConditionalWeakTable<object, ValueBox<PropertyType>> mProperties = new ConditionalWeakTable<object, ValueBox<PropertyType>>();

        /// <summary>
        /// Checks whether or not the given object has a value for this attached
        /// property.
        /// </summary>
        /// <param name="aOwningObject">
        /// The object instance to check.
        /// </param>
        /// <returns>
        /// True if this object
        /// </returns>
        public bool hasValue(object aOwningObject)
        {
            // Throwing away the value since we only want to check that 
            // the value exists.
            return this.mProperties.TryGetValue(aOwningObject, out _);
        }

        /// <summary>
        /// Gets the value of the attached property for the given object instance.
        /// </summary>
        /// <param name="aOwningObject">
        /// The object to get the attached property value for.
        /// </param>
        /// <returns>
        /// The value of the attached property for that object instance, or 
        /// the default value of <see cref="PropertyType"/> if not value was 
        /// set.
        /// </returns>
        public PropertyType getValue(object aOwningObject)
        {
            if (this.mProperties.TryGetValue(aOwningObject, out var lValueBox))
            {
                return lValueBox;
            }

            return default;
        }

        /// <summary>
        /// Sets the value of the attached property on the given object instance.
        /// If the property already has a value, it will be replaced.
        /// </summary>
        /// <param name="aOwningObject">
        /// The object to attach the property value to.
        /// </param>
        /// <param name="aValue">
        /// The value to attach.
        /// </param>
        public void setValue(object aOwningObject, PropertyType aValue)
        {
            var lBox = this.mProperties.GetOrCreateValue(aOwningObject);
            lBox.Value = aValue;
        }
    }
}
