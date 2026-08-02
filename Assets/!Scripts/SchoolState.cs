public enum SchoolState
{
    BeforeSchool,
    Class,
    Recess,
    AfterSchool,
    Lunch   // NEW. Added at the END so existing serialized values don't shift.
            // NPCs + player treat Lunch exactly like Recess (it's just "not Class").
            // You can set the Lunch period's state to Lunch OR Recess — both work.
}