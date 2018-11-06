/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 17 February 2018
*/

using System;
using UnityEngine;
using System.Collections;

/// <summary>
/// Extension here.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Take a range from array
    /// </summary>
    /// <typeparam name="T">type</typeparam>
    /// <param name="data">Target array</param>
    /// <param name="index">start index</param>
    /// <param name="length">take length</param>
    /// <returns></returns>
    public static T[] Get <T>(this T[] data, int index, int length)
    {
        T[] result = new T[length];
        Array.Copy(data, index, result, 0, length);
        return result;
    }
}

public static class MonoBehaviourExtensions
{
    public static void Invoke(this MonoBehaviour me, Action theDelegate, float time)
    {
        me.StartCoroutine(ExecuteAfterTime(theDelegate, time));
    }

    private static IEnumerator ExecuteAfterTime(Action theDelegate, float delay)
    {
        yield return new WaitForSeconds(delay);
        theDelegate();
    }
}
