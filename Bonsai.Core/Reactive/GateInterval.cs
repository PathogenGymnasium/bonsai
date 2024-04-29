using System;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Xml;
using System.Reactive.Linq;

namespace Bonsai.Reactive
{
    /// <summary>
    /// Represents an operator that allows a single element from the first sequence
    /// to pass through every time the specified time interval elapses.
    /// </summary>
    [DefaultProperty(nameof(Interval))]
    [XmlType(Namespace = Constants.XmlNamespace)]
    [Description("Allows a single element from the first sequence to pass through every time the specified interval elapses.")]
    public class GateInterval : Combinator
    {
        /// <summary>
        /// Gets or sets the period after which a new element from the sequence is
        /// allowed to pass through the gate.
        /// </summary>
        [XmlIgnore]
        [Description("The period after which a new element from the sequence is allowed to pass through the gate.")]
        public TimeSpan Interval { get; set; }

        /// <summary>
        /// Gets or sets an XML representation of the interval for serialization.
        /// </summary>
        [Browsable(false)]
        [XmlElement(nameof(Interval))]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public string IntervalXml
        {
            get { return XmlConvert.ToString(Interval); }
            set { Interval = XmlConvert.ToTimeSpan(value); }
        }

        /// <summary>
        /// Gets or sets a value specifying the maximum time the gate stays open.
        /// </summary>
        /// <remarks>
        /// If no value is specified, the gate stays open indefinitely until an element
        /// arrives. If a maximum due time is specified, however, then if an element
        /// from the first sequence arrives after this interval elapses, that element
        /// will not be allowed through and will be dropped from the result sequence.
        /// </remarks>
        [XmlIgnore]
        [Description("The maximum time the gate stays open.")]
        public TimeSpan? DueTime { get; set; }

        /// <summary>
        /// Gets or sets an XML representation of the gate due time for serialization.
        /// </summary>
        [Browsable(false)]
        [XmlElement(nameof(DueTime))]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public string DueTimeXml
        {
            get
            {
                var timeSpan = DueTime;
                if (timeSpan.HasValue) return XmlConvert.ToString(timeSpan.Value);
                else return null;
            }
            set
            {
                if (!string.IsNullOrEmpty(value)) DueTime = XmlConvert.ToTimeSpan(value);
                else DueTime = null;
            }
        }

        /// <summary>
        /// Allows a single element from an observable sequence to pass through every
        /// time the specified time interval elapses.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of the elements in the <paramref name="source"/> sequence.
        /// </typeparam>
        /// <param name="source">The observable sequence to filter.</param>
        /// <returns>
        /// The filtered observable sequence. Every time the specified time interval
        /// elapses, the next element from the <paramref name="source"/> sequence
        /// will be allowed through.
        /// </returns>
        public override IObservable<TSource> Process<TSource>(IObservable<TSource> source)
        {
            var dueTime = DueTime;
            var scheduler = HighResolutionScheduler.Default;
            var interval = Observable.Timer(Interval, scheduler);
            //var interval = Observable.Interval(Interval, scheduler); // Alternate fix
            /* I have this partially figured out:
             * What changed is that this indirectly went from using the
             * Oversvable.Window(Func<IObservable<>> windowClosingSelector) overload to the
             * Observable.Window(IObservable<> windowBoundaries) overlad.
             * 
             * Both of these work by using the timing of the results from the selector to determine where window boundaries lie
             * The difference is that the Func<T> overload re-subscribes to the source every single time.
             * The latter subscribes once and relies on the source periodically putting out values to signal window boundaries.
             * 
             * A relative Timer resets every time it is re-subscribed, so it works with the first overload even though it's the same IObservable every time.
             * The second one only reacts once since the timer only provides a single value after the time elapses
             * The second one works when Observable.Interval is used since Interval is the same as timer but repeats every time it expires
             * 
             * Unclear what the proper fix is, I need to read the Bonsai documentation more to get an understanding of what the DueTime variant is meant to do.
             */
            if (dueTime.HasValue)
                return source.Gate(interval, dueTime.Value, scheduler);
            else
                return source.Gate(interval);
        }
    }
}
