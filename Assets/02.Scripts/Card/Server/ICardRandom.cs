using System;

// 카드 셔플용 난수
public interface ICardRandom
{
    // 0 이상 maxExclusive 미만의 정수
    int Next(int maxExclusive);
}

// 기본 구현
public class SystemCardRandom : ICardRandom
{
    readonly Random random;

    public SystemCardRandom()
    {
        random = new Random();
    }

    // 디버깅용
    public SystemCardRandom(int seed)
    {
        random = new Random(seed);
    }

    public int Next(int maxExclusive) => random.Next(maxExclusive);
}
