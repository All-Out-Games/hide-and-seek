using AO;

public class References : Component
{
	[Serialized] public Texture ZooImage;
	private static References instance; 
	public static References Instance
	{
		get
		{
			if (instance == null)
			{
				instance = Entity.FindByName("GameManager").GetComponent<References>();
			}
			return instance;
		}
		private set => instance = value;
	} 
	
	public override void Awake()
	{
		
	}
}