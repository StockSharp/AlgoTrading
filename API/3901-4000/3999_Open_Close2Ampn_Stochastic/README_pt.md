# Estratégia Open Close2 Ampn Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Porta do MetaTrader 4 expert *open_close2ampnstochastic_strategy* reconstruída em cima do StockSharp API de alto nível.
- Usa um oscilador Stochastic clássico (comprimento 9, suavização 3/3) junto com um filtro de ação de preço de duas barras: a vela atual deve continuar na direção da anterior antes que um pedido seja enviado.
- Projetado para negociação em posição única. A fonte de vela padrão é uma hora, mas qualquer período de tempo pode ser conectado por meio do parâmetro `CandleType`.

## Lógica de Sinais
1. **Proteção de entrada** – apenas uma posição pode ser aberta por vez. Quando a estratégia é plana, ela avalia a última vela totalmente formada:
   - **Entrada longa** quando a linha principal Stochastic está acima da linha de sinal *e* tanto a abertura quanto o fechamento da última vela estão abaixo de seus valores anteriores (continuação da pressão descendente seguida pela força do oscilador).
   - **Entrada curta** quando a linha principal Stochastic está abaixo da linha de sinal *e* a vela mostra uma abertura e um fechamento mais altos que o anterior (empurrão para cima com confirmação do oscilador de baixa).
2. **Regras de saída** – enquanto existir uma posição, as mesmas condições são espelhadas ao contrário:
   - **Fechar comprado** quando a linha principal cai abaixo da linha de sinal e a nova vela imprime preços de abertura/fechamento mais altos.
   - **Fechar vendido** quando a linha principal sobe acima da linha de sinal e a nova vela imprime preços de abertura/fechamento mais baixos.
3. **Rebaixamento de proteção** – replica a saída de emergência MT4: se a magnitude da perda flutuante (PnL realizado + estimativa atual baseada em velas) atingir `MaximumRisk × account_margin`, a estratégia liquida a posição imediatamente. StockSharp não expõe *AccountMargin* de MetaTrader, então a porta se aproxima dele via `Portfolio.BlockedValue` e retorna para `Portfolio.CurrentValue` quando a margem bloqueada não está disponível.

## Gestão de capital
- **BaseVolume** reflete a entrada original `Lots` e é usado sempre que nenhuma informação da conta estiver disponível.
- Se existir avaliação de portfólio, o tamanho bruto do pedido se tornará `Portfolio.CurrentValue × MaximumRisk / 1000`, correspondendo ao dimensionamento original baseado em `AccountFreeMargin`.
- Após cada negociação perdida, a próxima posição é reduzida em `losses / DecreaseFactor`; o contador de sequências é reiniciado após uma negociação lucrativa. O tamanho resultante nunca pode cair abaixo de `MinimumVolume`, cujo padrão é 0,1 lote, como o script MQL.
- Todos os volumes calculados são alinhados com os limites do instrumento (`VolumeStep`, `MinVolume`, `MaxVolume`) antes de enviar ordens de mercado.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `BaseVolume` | decimal | `0.1` | Tamanho do pedido substituto quando o dimensionamento baseado em risco não pode ser calculado. |
| `MaximumRisk` | decimal | `0.3` | Fração do patrimônio utilizado tanto para dimensionamento dinâmico quanto para guarda de rebaixamento. Defina como `0` para desativar os cálculos de risco. |
| `DecreaseFactor` | decimal | `100` | Divisor aplicado após perdas consecutivas. Valores mais altos retardam a redução. |
| `MinimumVolume` | decimal | `0.1` | Piso absoluto para o volume calculado. |
| `StochasticLength` | interno | `9` | Período de retrospectiva do oscilador Stochastic. |
| `StochasticKLength` | interno | `3` | Período de suavização da linha %K. |
| `StochasticDLength` | interno | `3` | Período de suavização da linha de sinal %D. |
| `CandleType` | `DataType` | `TimeFrame(1h)` | Fonte de vela usada para acionar o indicador e os filtros de preço. |

## Notas de implementação
- O PnL flutuante exigido pela saída de emergência é estimado com o último fechamento da vela e `Strategy.PositionPrice`. Isso reflete a intenção de `AccountProfit` em MetaTrader, mas os cálculos reais do lado do corretor podem ser diferentes.
- Se nem a margem bloqueada nem o valor do portfólio forem expostos pelo conector, a guarda de rebaixamento permanecerá ociosa enquanto a estratégia ainda negociar usando `BaseVolume`.
- `StartProtection()` é ativado na inicialização para que os mecanismos de proteção de StockSharp (roteamento stop/take, reconexões) espelhem a rede de segurança presente na versão MQL.

## Diferenças do especialista original
- O arredondamento de lote MetaTrader é emulado usando os metadados do instrumento disponíveis em StockSharp. Verifique os valores `VolumeStep`/`MinVolume` do título negociado para que o tamanho da posição corresponda às restrições do corretor.
- O código MT4 avaliou tick por tick enquanto protegia com `Volume[0]`. A porta processa apenas velas concluídas, o que evita sinais duplicados e é o padrão recomendado para estratégias StockSharp.
- As métricas da conta são aproximações; se você depende de limites de margem estritos, ajuste `MaximumRisk` ou substitua a proteção para corresponder às fórmulas exatas do corretor.
