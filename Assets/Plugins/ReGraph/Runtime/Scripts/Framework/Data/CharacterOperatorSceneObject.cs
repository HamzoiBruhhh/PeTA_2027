namespace Reshape.ReFramework
{
    public struct CharacterOperatorSceneObject
    {
        public CharacterOperator character;
        public int index;
        public float value;

        public CharacterOperatorSceneObject (CharacterOperator op, int i)
        {
            character = op;
            index = i;
            value = default;
        }
    }
}