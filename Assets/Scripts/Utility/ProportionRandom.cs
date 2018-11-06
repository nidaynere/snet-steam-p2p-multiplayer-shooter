/*! 
@author https://stackoverflow.com/questions/3655430/selection-based-on-percentage-weighting
*/

using System.Collections.Generic;
using System;

public class ProportionValue<T>
{
    public double Proportion { get; set; }
    public T Value { get; set; }
}

/// <summary>
/// Let's say the proportion consists with 3 objects.
/// Dagger, rate = 50
/// Sword, rate = 90
/// Pistol, rate = 60
/// ChooseByRandom will return Pistol(with %30 chance) Sword (with %45 chance) Dagger (with %25 chance)
/// </summary>
public static class ProportionValue
{
    public static ProportionValue<T> Create<T>(double proportion, T value)
    {
        return new ProportionValue<T> { Proportion = proportion, Value = value };
    }

    static Random random = new Random();
    public static T ChooseByRandom<T>(
        this IEnumerable<ProportionValue<T>> collection)
    {
		double total = 0;

        foreach (var item in collection)
        {
            total += item.Proportion;
        }

		foreach (var item in collection)
			item.Proportion = item.Proportion / total;
		
        var rnd = random.NextDouble();

        foreach (var item in collection)
        {
            if (rnd < item.Proportion)
                return item.Value;
            rnd -= item.Proportion;
        }
        throw new InvalidOperationException(
            "The proportions in the collection do not add up to 1.");
    }
}