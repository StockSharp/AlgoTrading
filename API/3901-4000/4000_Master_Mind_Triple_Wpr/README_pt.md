# Estratégia Triple WPR Master Mind
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Porta do MetaTrader 4 consultor especialista `MasterMind3CE` (pasta `MQL/8458`).
- Usa quatro indicadores Williams %R com períodos 26, 27, 29 e 30 para detectar condições extremas de sobrecompra/sobrevenda.
- Projetado para entradas de reversão à média: comprar após uma liquidação profunda, vender após uma alta prolongada.
- Inclui lógica configurável de stop-loss, take-profit e trailing-stop opcional, expressa em etapas de preço do instrumento.
- Funciona em qualquer período de tempo suportado pelo terminal StockSharp conectado; o padrão são velas de 15 minutos.

## Lógica de negociação
### Indicadores
- `WilliamsR(26)` — oscilador extremamente rápido.
- `WilliamsR(27)` — oscilador rápido para confirmação.
- `WilliamsR(29)` — oscilador médio que suaviza o sinal.
- `WilliamsR(30)` — oscilador lento que requer valores extremos em vários lookbacks.

Todos os quatro osciladores devem ser formados. A assinatura processa apenas velas concluídas para corresponder ao comportamento `TradeAtCloseBar = true` do especialista original.

### Condições de Entrada
- **Entrada longa**: todos os quatro valores Williams %R são inferiores ou iguais a `OversoldLevel` (padrão `-99.99`). A estratégia visa uma posição longa de `TradeVolume`. Se uma posição vendida estiver aberta, ela será fechada e convertida em comprada em uma única ordem de mercado dimensionada para atingir a exposição desejada.
- **Entrada curta**: todos os quatro valores Williams %R estão acima ou iguais a `OverboughtLevel` (padrão `-0.01`). A estratégia visa uma posição curta de `TradeVolume`, fechando primeiro qualquer exposição longa existente.

### Condições de saída
- **Saída baseada em sinal**: Quando uma posição longa está aberta e uma condição de entrada curta aparece, a estratégia fecha/inverte a posição (e vice-versa).
- **Stop-loss de proteção**: distância opcional do passo de preço aplicada a partir do preço médio de entrada. Uma batida na máxima/mínima da vela desencadeia uma saída do mercado.
- **Take-profit**: meta de preço opcional a partir do preço médio de entrada. Uma vez alcançada a vela, a posição é fechada.
- **Trailing-stop**: Lógica móvel opcional que começa quando o preço se move `TrailingStopSteps + TrailingStepSteps` a favor. O stop é então mantido a `TrailingStopSteps` de distância do último fechamento e só avança quando melhorado em pelo menos `TrailingStepSteps`.

## Gestão de risco
As distâncias de preço são especificadas nas *etapas de preço* do instrumento. Por exemplo, com `PriceStep = 0.0001` e `StopLossSteps = 2000`, a parada é colocada a 0,2000 da entrada. A estratégia recalcula o preço médio de entrada ao escalar na mesma direção para manter os níveis de risco consistentes. Os trailing stops são desativados, a menos que `TrailingStopSteps` e `TrailingStepSteps` sejam positivos.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `TradeVolume` | Tamanho da posição líquida alvo (lotes/contratos). | `1` |
| `OversoldLevel` | Williams Limite de %R que confirma condições de sobrevenda. | `-99.99` |
| `OverboughtLevel` | Williams Limite de %R que confirma condições de sobrecompra. | `-0.01` |
| `StopLossSteps` | Distância de stop-loss em `PriceStep` unidades. Defina `0` para desativar. | `2000` |
| `TakeProfitSteps` | Distância de lucro em `PriceStep` unidades. Defina `0` para desativar. | `0` |
| `TrailingStopSteps` | Distância da parada móvel em `PriceStep` unidades. Requer `TrailingStepSteps > 0`. | `0` |
| `TrailingStepSteps` | Melhoria mínima antes do trailing stop ser movido (em `PriceStep` unidades). | `1` |
| `CandleType` | Tipo de dados/período de vela processado pela estratégia. | `TimeFrame(15m)` |

## Notas de conversão
- Alertas, notificações sonoras, registro em arquivos e recursos de e-mail do especialista MQL são omitidos intencionalmente; StockSharp registros podem ser usados.
- O consultor original permitiu a negociação antes do fechamento da barra. A porta mantém a lógica padrão de “negociação ao fechar”, processando apenas velas finalizadas.
- Números mágicos, tentativas repetidas de pedidos e desenho manual de objetos eram específicos de MetaTrader e não têm equivalentes diretos de StockSharp, portanto, foram removidos.
- A gestão de riscos é consolidada dentro da estratégia, em vez de utilizar ciclos externos de modificação de ordens; as verificações stop/take são avaliadas em cada vela.

## Uso
1. Configure o instrumento e o prazo desejados, de acordo com o gráfico ao qual o especialista estava originalmente anexado.
2. Ajuste limites ou parâmetros de risco se o instrumento tiver um perfil de volatilidade diferente.
3. Lançar a estratégia; ele assinará a série de velas especificada, monitorará Williams %R extremos e gerenciará as posições de acordo.
