using UnityEngine;

public class ShowIfAttribute : PropertyAttribute
{
    public string condition;
    public int value;

    public ShowIfAttribute(string condition, int value)
    {
        this.condition = condition;
        this.value = value;
    }
}
