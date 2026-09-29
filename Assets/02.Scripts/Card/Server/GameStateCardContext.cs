// ICardContext의 구현체
// GameManager를 최대한 건들지 않기 위해 이 클래스에서 GameState를 읽어 카드 로직에 전달
// 추후 서버로 이동
public class GameStateCardContext : ICardContext
{
    readonly GameManager gameManager;

    public GameStateCardContext(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    public int BoardSize => 32;
    public int InspectionTileId => 8; // TODO: 보드 담당에게 무인도 칸 번호 확인 (BoardTileType에 아직 무인도 타입 없음)

    public int GetPosition(long playerId)
    {
        foreach (var player in gameManager.gameState.PlayerStates)
        {
            if (player.PlayerId == playerId)
                return player.Position;
        }

        return 0;
    }
}
