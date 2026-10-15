# Estratégia Casino111
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Casino111 é um sistema de contra-tendência que se origina do MetaTrader 4 consultor especialista com o mesmo nome. Em cada nova barra, a estratégia compara o preço de abertura atual com os níveis de referência derivados da vela diária anterior. Se os gaps abertos ultrapassarem os extremos diários (mais buffers configuráveis), o algoritmo abre imediatamente uma posição de mercado na direção oposta e conta com proteção simétrica de stop-loss/take-profit. A porta StockSharp mantém o comportamento de posição única do robô original e adiciona ampla parametrização para pesquisa e otimização.

## Lógica de entrada e saída
1. As máximas e mínimas diárias anteriores são recuperadas de uma assinatura diária dedicada de velas. Dois deslocamentos (`UpperOffsetPoints` e `LowerOffsetPoints`) expressos em MetaTrader pontos expandem o canal de referência.
2. Em cada vela de negociação finalizada, a estratégia inspeciona as aberturas anteriores e atuais:
   - Quando a nova abertura salta acima da máxima diária mais o deslocamento superior, uma posição **curta** é aberta (desvanecimento do gap).
   - Quando a nova abertura cai abaixo da mínima diária menos o deslocamento inferior, uma posição **longa** é aberta.
3. Apenas uma posição é permitida por vez. Quaisquer ordens ativas devem ser preenchidas antes que um novo sinal seja considerado.
4. `StartProtection` espelha o stop fixo original e o take target, ambos localizados a `BetPoints` de distância do preço de entrada (convertido em etapas de preço).

## Gestão de dinheiro
- `UseMoneyManagement = false` mantém o tamanho da negociação fixo (`BaseVolume`).
- `UseMoneyManagement = true` ativa a progressão martingale vista no código MT4:
  - Após cada negociação com perda ou ponto de equilíbrio, o próximo volume do pedido é multiplicado por `(BetPoints * 2) / (BetPoints - spreadPoints)`.
  - O spread é estimado a partir das últimas melhores cotações de compra/venda coletadas por meio da assinatura do livro de pedidos. Quando não há cotações disponíveis, o padrão do multiplicador é `2`.
  - As vitórias redefinem o tamanho da posição para `BaseVolume`. Todos os volumes estão alinhados ao instrumento `VolumeStep` e limitados por `MaxVolume`.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `EnableBuy` | `bool` | `true` | Permitir entradas longas acionadas por lacunas abaixo do canal diário. |
| `EnableSell` | `bool` | `true` | Permitir entradas curtas acionadas por lacunas acima do canal diário. |
| `BetPoints` | `decimal` | `400` | Distância simétrica de stop-loss e take-profit em MetaTrader pontos (convertidos em etapas de preço para StockSharp). |
| `UpperOffsetPoints` | `decimal` | `97` | Buffer adicionado acima da alta diária anterior para detectar reversões de gap de baixa. |
| `LowerOffsetPoints` | `decimal` | `77` | Buffer subtraído abaixo da mínima diária anterior para detectar reversões de gap de alta. |
| `UseMoneyManagement` | `bool` | `false` | Habilite a progressão de lote no estilo martingale. |
| `MaxVolume` | `decimal` | `4` | Teto aplicado ao volume calculado quando a gestão de dinheiro está ativa. |
| `BaseVolume` | `decimal` | `0.1` | Tamanho da ordem inicial usado após uma negociação lucrativa ou quando o gerenciamento de dinheiro está desativado. |
| `CandleType` | `DataType` | `H1` | Período primário usado para avaliar as condições de gap aberto (o padrão é 1 hora). |
| `DailyCandleType` | `DataType` | `D1` | Tipo de vela que fornece a máxima/mínima do dia anterior (o padrão é 1 dia). |

## Notas de implementação
- A estratégia depende do API de alto nível de StockSharp: `SubscribeCandles` fornece fluxos diários e de negociação, enquanto `SubscribeOrderBook` mantém o spread mais recente para o multiplicador de gerenciamento de dinheiro.
- `StartProtection` gerencia as pernas stop-loss e take-profit, de modo que cada entrada recebe imediatamente saídas simétricas, assim como no MT4.
- Os comentários embutidos em inglês destacam cada ponto de decisão para facilitar a manutenção.
- Todos os cálculos evitam pesquisas no histórico do indicador; apenas os valores atuais de abertura da vela são necessários, espelhando a lógica `Time[0]` / `Open[0]` de MetaTrader.

## Dicas de uso
- Escolha um período de negociação que corresponda ao seu estudo. As velas padrão de uma hora replicam a configuração MT4 comum, mas qualquer `DataType` suportado por StockSharp pode ser fornecido.
- Ao usar o gerenciamento de dinheiro, certifique-se de que `MaxVolume` respeite os limites do corretor; o auxiliar de alinhamento fixa o resultado em `VolumeStep`, `MinVolume` e `MaxVolume`.
- Como o sistema sempre mantém no máximo uma posição aberta, ele combina bem com gráficos StockSharp que traçam marcadores de entrada/saída para inspeção manual.
- Teste a estratégia dentro de um ambiente de replay antes de conectá-la a um local ao vivo – a abordagem de redução de lacunas é agressiva e depende de spreads confiáveis.
