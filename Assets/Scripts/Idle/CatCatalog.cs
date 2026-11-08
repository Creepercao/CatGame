namespace CatGame.Idle
{
    public sealed class CatDefinition
    {
        public string id, name, path;
        public int stage, pixels, maxOwned = IdleBalance.CatMaxOwned;
        public double unlock, income, firstPrice, priceGrowth = IdleBalance.CatPriceGrowth, scale;
    }

    public static class CatCatalog
    {
        public static readonly CatDefinition[] All = {
            Cat("Milk","墨墨",1,0,10,50,64), Cat("Tabby","小棕",1,0,16,70,64), Cat("Snow","雪球",1,0,24,100,64), Cat("Calico","三花",1,0,34,140,64),
            Cat("Tuxedo","绅士",2,1e5,95,400,64), Cat("Siamese","暹罗",2,1e5,145,600,64), Cat("Tiger","虎纹",2,1e5,220,900,64),
            Cat("Egypt","埃及猫",3,1e7,980,4000,64), Cat("Batcat","蝙蝠侠",3,1e7,1480,6000,64), Cat("Demon","小恶魔",3,1e7,2230,9000,64), Cat("Xmas","圣诞猫",4,1e9,10000,40000,64)
        };
        static CatDefinition Cat(string id,string name,int stage,double unlock,double income,double price,int pixels)
        {
            var scale = IdleBalance.CatStageScales[stage - 1];
            return new CatDefinition { id=id,name=name,stage=stage,unlock=0,income=income*scale,firstPrice=price*IdleBalance.CatPriceScales[stage - 1],pixels=pixels,scale=64.0/pixels,path="Art/Cats/"+id };
        }
    }
}
