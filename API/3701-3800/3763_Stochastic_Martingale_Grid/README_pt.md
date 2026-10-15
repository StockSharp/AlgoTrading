# Stochastic Martingale Estratégia de grade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma versão StockSharp do MetaTrader consultor especialista `rmkp_9yj4qp1gn8fucubyqnvb`. Ele combina um filtro de entrada de oscilador estocástico com uma grade de média estilo martingale. O algoritmo monitora as velas finalizadas, espera que a linha do sinal estocástico saia das zonas de sobrecompra ou sobrevenda predefinidas e, em seguida, abre uma posição na direção da reversão. Quando o preço se move contra a negociação, ele adiciona ordens médias com volume duplicado em distâncias fixas de pip. Cada perna carrega sua própria meta de lucro e gerenciamento de trailing stop, permitindo que as posições aumentem de forma independente assim que o preço se recuperar.

## Lógica de negociação
- **Detecção de sinal:**
  - As linhas %K e %D de um oscilador estocástico configurável são avaliadas em velas concluídas.
  - Uma configuração longa é acionada quando, na vela anterior, %K estava acima de %D e %D estava abaixo do limite `ZoneBuy`.
  - Uma configuração curta é acionada quando, na vela anterior, %K estava abaixo de %D e %D estava acima do limite `ZoneSell`.
- **Execução inicial:**
  - Com um sinal válido e enquanto a conta estiver estável, a estratégia envia uma ordem de mercado com o `BaseVolume`.
  - O preço de entrada é armazenado para gerenciar os trailing stops e posteriormente calcular a média dos pedidos.
- **Martingale média:**
  - Enquanto uma posição permanece aberta, o algoritmo observa o movimento adverso do preço de `StepPips` em relação à última ordem preenchida.
  - Cada nova ordem média duplica o volume da perna anterior (progressão martingale clássica) e só é colocada se o número total de pernas abertas for inferior a `MaxOrders` e a negociação continuar permitida.
- **Gerenciamento de saída:**
  - Cada etapa define um nível de lucro individual localizado a `TakeProfitPips` de seu preço de entrada.
  - Os trailing stops são ativados quando o lucro não realizado atinge `TrailingStopPips`; a âncora final é apertada sempre que os lucros se estendem ainda mais.
  - Se o preço retornar ao nível final ou atingir o nível de take-profit, a perna correspondente será fechada enquanto o restante do cluster permanecerá ativo.
  - Quando todas as pernas saem, a estratégia redefine seu estado interno e aguarda o próximo sinal estocástico.

## Gestão de risco
- A expansão martingale é limitada por `MaxOrders` e pelos limites de volume de segurança.
- Os volumes são normalizados para o `VolumeStep` do instrumento e as restrições de volume mínimo/máximo são respeitadas.
- Os trailing stops ajudam a proteger os lucros flutuantes de reversões totais.

## Parâmetros
| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| `CandleType` | Assinatura de velas usada para cálculos de indicadores. | Período de 15 minutos |
| `BaseVolume` | Volume de pedido inicial colocado no primeiro sinal. | `0.1` |
| `TakeProfitPips` | Distância pip entre cada preço de entrada e sua meta de lucro. | `50` |
| `TrailingStopPips` | Distância pip usada para ativação e rastreamento do trailing stop por perna. | `20` |
| `MaxOrders` | Número máximo de trechos médios simultâneos (incluindo a entrada inicial). | `7` |
| `StepPips` | Movimento adverso mínimo, em pips, necessário antes de adicionar outra ordem de média. | `7` |
| `KPeriod` | Comprimento de lookback para a linha %K estocástica. | `5` |
| `DPeriod` | Comprimento de suavização para a linha %D estocástica. | `3` |
| `Slowing` | Suavização adicional aplicada ao cálculo de %K. | `3` |
| `ZoneBuy` | Limite superior que permite configurações longas quando %K está acima de %D. | `30` |
| `ZoneSell` | Limite inferior que permite configurações curtas quando %K está abaixo de %D. | `70` |

## Notas
- A estratégia usa o StockSharp API de alto nível com assinaturas de velas e vinculações de indicadores, mantendo a implementação próxima da lógica MetaTrader original enquanto aproveita as ferramentas de risco e visualização de StockSharp.
- Como a média das negociações dobra o volume, certifique-se de que o volume máximo permitido do instrumento possa acomodar a escada martingale.
- Tal como acontece com qualquer sistema martingale, a gestão adequada do capital e restrições de risco adicionais são altamente recomendadas antes da implementação numa conta real.
