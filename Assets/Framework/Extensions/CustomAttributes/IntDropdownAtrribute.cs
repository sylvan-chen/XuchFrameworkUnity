using UnityEngine;

namespace DigiEden.Extensions
{
    public class IntDropdownAttribute : PropertyAttribute
    {
        public readonly int[] Options;

        public IntDropdownAttribute(params int[] options)
        {
            Options = options;
        }
    }
}