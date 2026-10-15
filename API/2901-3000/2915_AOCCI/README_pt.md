# Estratégia AOCCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Conversão do consultor especializado MetaTrader 5 `AOCCI` para a API de alto nível do StockSharp.
- Combina o Awesome Oscillator e o Commodity Channel Index com um simples filtro de nível pivô.
- Inclui proteção de spread através de filtros de "salto grande" e "salto duplo" para ignorar ação de preço instável.
- Reproduz a lógica MQL5 original onde a configuração vendida usa as mesmas condições que a configuração comprada.

## Dados e indicadores
- Usa o período primário definido por `CandleType` para geração de sinais.
- Assina um período superior adicional (`HigherCandleType`, padrão 1 hora) para ler o fechamento anterior como filtro de tendência.
- Indicadores:
  - `AwesomeOscillator` para detectar a direção do impulso.
  - `CommodityChannelIndex` com período configurável e deslocamento de sinal opcional.
- Calcula um nível pivô da vela localizada em `SignalCandleShift + 1` no período de trabalho: `(High + Low + Close) / 3`.

## Lógica de entrada
1. Aguardar até que ambos os indicadores estejam completamente formados e pelo menos seis velas concluídas estejam disponíveis.
2. Coletar valores CCI com o deslocamento configurado (`SignalCandleShift` para a comparação atual e `SignalCandleShift + 1` para a barra anterior).
3. Rejeitar a barra quando qualquer filtro de salto for acionado:
   - `BigJumpPips` compara preços de abertura consecutivos dos últimos cinco intervalos.
   - `DoubleJumpPips` compara preços de abertura separados por uma barra.
4. Entrada comprada quando todas as condições abaixo são satisfeitas e não há posição ativa:
   - O Awesome Oscillator é positivo na barra atual.
   - O valor CCI deslocado é maior ou igual a zero.
   - O preço de fechamento atual está acima do nível pivô.
   - Pelo menos uma confirmação é baixista nos dados anteriores: valor AO anterior abaixo de zero, CCI deslocado anterior ≤ 0, ou o último fechamento do período superior abaixo do pivô.
5. A entrada vendida usa exatamente o mesmo conjunto de regras que a entrada comprada (o especialista original contém condições idênticas para ambas as direções).

## Lógica de saída e gestão de risco
- Quando uma operação é aberta, níveis opcionais de stop-loss e take-profit são atribuídos usando as distâncias em pips configuradas multiplicadas pelo tamanho de pip detectado do instrumento.
- Em cada vela concluída, a estratégia verifica se os níveis de take-profit ou stop-loss foram atingidos usando os extremos da vela e fecha a posição a mercado.
- O trailing stop é ativado quando tanto `TrailingStopPips` quanto `TrailingStepPips` são positivos:
  - Operações compradas movem o stop para `Close - TrailingStopPips` assim que o preço avança pelo menos `TrailingStopPips + TrailingStepPips` desde a entrada.
  - Operações vendidas movem o stop para `Close + TrailingStopPips` assim que o preço cai a mesma distância combinada.
- Se uma posição for fechada (por stop, alvo ou trailing), a estratégia aguarda até a próxima vela para avaliar novas entradas.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| `TradeVolume` | 1 | Volume de ordem base usado para entradas de mercado. |
| `StopLossPips` | 50 | Distância em pips para o stop protetor. Definir como 0 para desabilitar. |
| `TakeProfitPips` | 50 | Distância em pips para o take-profit. Definir como 0 para desabilitar. |
| `TrailingStopPips` | 5 | Distância do trailing stop em pips. Requer `TrailingStepPips` > 0. |
| `TrailingStepPips` | 5 | Buffer adicional antes do trailing stop ser atualizado. |
| `CciPeriod` | 55 | Período do Commodity Channel Index. |
| `SignalCandleShift` | 0 | Deslocamento ao ler o buffer CCI e a vela pivô. |
| `BigJumpPips` | 100 | Diferença máxima permitida (em pips) entre aberturas consecutivas das últimas velas. |
| `DoubleJumpPips` | 100 | Diferença máxima permitida (em pips) entre cada segunda abertura de vela. |
| `CandleType` | velas de 15 minutos | Período de trabalho para os sinais primários. |
| `HigherCandleType` | velas de 1 hora | Período superior usado para obter o fechamento anterior de confirmação. |

## Notas
- O tamanho do pip é derivado de `Security.PriceStep` e ajustado para instrumentos cotados com 3 ou 5 dígitos decimais.
- Como o EA original usou filtros idênticos para ambas as direções, operações vendidas só ocorrerão se a condição comprada também for satisfeita e a estratégia puder vender. Desabilitar operações vendidas externamente se não for desejado.
- Os filtros de salto requerem pelo menos seis velas concluídas antes que a primeira operação seja avaliada.
