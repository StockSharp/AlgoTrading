# Estratégia de balanço do pêndulo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Pendulum Swing Strategy** é uma versão StockSharp do consultor especialista MetaTrader *Pendulum 1_01*. O sistema original mantém duas ordens de stop pendentes em torno do preço atual e aumenta progressivamente o seu volume após cada preenchimento. Esta versão C# reproduz o mesmo comportamento de "swing" usando auxiliares StockSharp de alto nível.

Ideias principais:

- Mantenha ordens simétricas de compra e venda a uma distância configurável da última vela fechada.
- Após cada preenchimento, a próxima parada do mesmo lado multiplica seu volume, implementando a progressão estilo martingale da fonte MQL.
- Feche a posição quando uma meta de pip de curto prazo for atingida ou quando o patrimônio da conta ultrapassar os limites globais de lucros/perdas.

## Como funciona
1. Quando a estratégia é iniciada, ela assina uma série de velas definidas pelo usuário (padrão: 15 minutos) e, opcionalmente, velas diárias. A faixa diária controla a distância entre o preço de mercado e os stops pendentes.
2. Em cada vela de negociação concluída, o algoritmo:
   - Atualiza os limites globais baseados em ações.
   - Verifica se a posição atual atingiu a meta de lucro local.
   - Calcula a distância do stop a partir do último intervalo diário ou da entrada manual do pip e, em seguida, coloca/atualiza as ordens buy-stop e sell-stop.
3. Quando uma ordem de stop é preenchida, o nível de progressão correspondente avança, de modo que a próxima parada desse lado utiliza o volume multiplicado. Depois que `MaxLevels` for alcançado, nenhum novo pedido será criado para essa direção até que a posição retorne a zero.
4. As verificações globais de take-profit/stop-loss são executadas após cada vela e liquidam o portfólio se os limites de patrimônio configurados forem violados.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| ---- | ---- | ------- | ----------- |
| `BaseVolume` | `decimal` | `0.1` | Volume do primeiro stop pendente. |
| `VolumeMultiplier` | `decimal` | `2` | Fator aplicado após cada nível preenchido no mesmo lado. |
| `MaxLevels` | `int` | `8` | Número máximo de preenchimentos permitidos em uma direção. |
| `ManualStepPips` | `int` | `50` | Distância de parada em pips quando a faixa diária não estiver disponível. |
| `UseDynamicRange` | `bool` | `true` | Se ativado, deriva o passo da última vela diária concluída. |
| `RangeFraction` | `decimal` | `0.2` | Fração da faixa diária usada como distância base de parada. |
| `TakeProfitPips` | `int` | `10` | Alvo pip local que fecha a posição atual. Defina `0` para desativar. |
| `SlippagePips` | `int` | `3` | Buffer extra adicionado à distância pendente para imitar o deslizamento MetaTrader. |
| `UseGlobalTargets` | `bool` | `true` | Permite verificações de liquidação baseadas em patrimônio. |
| `GlobalTakePercent` | `decimal` | `1` | Crescimento do capital (em percentagem) que desencadeia a realização de lucros globais. |
| `GlobalStopPercent` | `decimal` | `2` | Rebaixamento do patrimônio (em porcentagem) que aciona o stop loss global. |
| `CandleType` | `DataType` | `15m` velas | Período usado para a lógica de negociação primária. |

## Notas
- O dimensionamento da posição respeita o passo de volume do instrumento e as configurações de volume mínimo e máximo.
- Os preços Stop se adaptam à etapa de preço do instrumento e evitam a substituição constante de pedidos, honrando uma tolerância de preço.
- Os alvos globais dependem de `Portfolio.CurrentValue` (ou `BeginValue` como substituto), portanto o portfólio selecionado deve expor essas informações.
- A estratégia usa `StartProtection()` para ativar a proteção de posição integrada de StockSharp uma vez na inicialização.

## Diferenças de conversão
- O desenho do rótulo da IU e as tabelas de saldo da conta do script MQL original são omitidos.
- Os níveis globais de lucro seguem limites de patrimônio baseados em porcentagem, em vez da aritmética bruta do valor do tick usada em MQL, mantendo o comportamento consistente entre os corretores.
- Funções específicas de MetaTrader, como `OrderModify`, são substituídas por rotinas de cancelamento e reenvio de pedidos StockSharp.
