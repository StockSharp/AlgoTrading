# Estratégia de Cronex RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia de Cronex RSI** recria o consultor especialista Exp_CronexRSI.mq5 na API de alto nível do StockSharp. A pilha de indicadores combina um Índice de Força Relativa (RSI) clássico com duas médias móveis sequenciais para reduzir o ruído. As decisões de trading são baseadas em cruzamentos entre as curvas RSI suavizadas rápida e lenta, com permissões de entrada/saída configuráveis que correspondem aos parâmetros MQL5 originais.

## Lógica de trading

1. Construir o RSI a partir do preço aplicado e do período de retrocesso selecionados.
2. Suavizar o valor RSI com uma média móvel *rápida*, depois suavizar o resultado com uma média móvel *lenta*.
3. Avaliar cruzamentos com um deslocamento de confirmação configurável:
   - Quando a curva rápida estava acima da curva lenta uma barra antes e cai abaixo na barra confirmada, a estratégia fecha posições vendidas e, se habilitado, abre uma posição comprada.
   - Quando a curva rápida estava abaixo da curva lenta e cruza acima na barra confirmada, a estratégia fecha compradas e pode entrar em operações vendidas.
4. Os volumes são simétricos em ambas as direções. Quando um novo sinal reverte a posição, a estratégia primeiro cobre a exposição existente e depois abre o novo lado usando o volume base configurado.

Por padrão a estratégia aguarda uma vela completamente fechada antes de agir sobre um sinal, reproduzindo o comportamento `SignalBar = 1` do Exp_CronexRSI. Definir o deslocamento para zero processa o cruzamento imediatamente na barra de fechamento.

## Parâmetros

| Nome | Descrição |
| ---- | ----------- |
| `RsiPeriod` | Período de retrocesso RSI. |
| `FastPeriod` | Comprimento da média móvel de suavização rápida. |
| `SlowPeriod` | Comprimento da segunda média móvel de suavização. |
| `SignalShift` | Número de barras concluídas usadas para confirmação (0 reage instantaneamente). |
| `SmoothingMethod` | Tipo de média móvel aplicado durante ambos os estágios de suavização (simples, exponencial, suavizada, ponderada linear, ponderada por volume). |
| `AppliedPrice` | Componente de preço passado ao RSI (fechamento, abertura, máximo, mínimo, mediana, típico, ponderado). |
| `CandleType` | Série de velas processada pela estratégia. |
| `TradeVolume` | Tamanho de ordem base usado para novas entradas. |
| `EnableLongEntry` / `EnableShortEntry` | Permitir abertura de posições compradas/vendidas. |
| `EnableLongExit` / `EnableShortExit` | Permitir fechamento de posições em resposta a sinais opostos. |

## Notas de implementação

- O método de suavização usa classes de média móvel do StockSharp. A opção `VolumeWeighted` também cobre os estilos VIDYA/AMA do MQL5 aplicando um substituto pragmático ponderado por volume.
- A seleção de preço aplicado corresponde às entradas do indicador Cronex e reflete o auxiliar usado dentro do consultor especialista original.
- Todos os valores de indicadores são processados através de instâncias `DecimalIndicatorValue` para permanecer compatíveis com o pipeline de indicadores do StockSharp enquanto evita polling direto de valores.
- A estratégia redimensiona automaticamente seu histórico interno quando o deslocamento de confirmação muda, garantindo que a lógica de cruzamento mantenha a estrutura exata de retrocesso da versão MQL5.

## Uso

1. Anexar a estratégia a uma carteira e ativo no designer do StockSharp ou via código.
2. Configurar o período de velas, estilo de suavização e permissões de trading para corresponder à configuração preferida do Cronex RSI.
3. Lançar a estratégia. Ela subscreverá a série de velas selecionada, atualizará a combinação RSI/MA e enviará ordens a mercado em cruzamentos confirmados.
4. Usar os auxiliares de gráfico integrados para visualizar as curvas de indicadores e operações executadas para validação adicional.
