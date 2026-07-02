# Estratégia de perfuração de nuvem escura CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma porta StockSharp do consultor MetaTrader Expert_ADC_PL_CCI. Ele verifica a ação do preço para reversões de velas Piercing Line e Dark Cloud Cover e usa o Commodity Channel Index (CCI) como confirmação. Uma vez detectado um padrão válido juntamente com uma leitura extrema de CCI, a estratégia abre uma posição de mercado na direção da reversão e posteriormente sai quando o CCI sai de sua zona extrema.

## Indicadores
- **Commodity Channel Index (CCI):** confirma os extremos do momentum e produz as condições de saída.
- **Comprimento médio do corpo (SMA):** mede o tamanho do corpo da vela para validar velas “longas” dentro da definição do padrão.
- **Preço médio de fechamento (SMA):** atua como um filtro de tendência simples que reflete a média móvel usada na lógica original MQL.

## Regras de negociação
### Entrada
- **Sinal de alta (Linha Perfurante):**
  1. A vela anterior deve ser uma vela longa de baixa que abre acima do seu fechamento.
  2. A última vela deve ser uma vela longa de alta que abre abaixo da mínima anterior e fecha dentro do corpo anterior, acima do seu ponto médio, mas abaixo da abertura anterior.
  3. O ponto médio da vela mais antiga deve estar abaixo da média móvel para confirmar uma tendência de baixa de curto prazo.
  4. O valor CCI concluído mais recentemente deve ser menor ou igual a `-EntryConfirmationLevel` (padrão `50`).
  5. Se existir uma posição curta, ela será totalmente fechada antes de entrar na posição comprada.
- **Sinal de baixa (Dark Cloud Cover):** lógica espelhada do sinal de alta com uma longa vela de alta seguida por uma longa vela de baixa que se abre, penetra no corpo anterior e fecha abaixo de seu ponto médio enquanto CCI é maior ou igual a `EntryConfirmationLevel`.

### Sair
- **Posições longas:** fechadas quando o CCI cruza abaixo de `ExitLevel` ou cruza abaixo de `-ExitLevel` de cima, sinalizando que o impulso se normalizou.
- **Posições curtas:** fechadas quando o CCI cruza acima de `-ExitLevel` ou acima de `ExitLevel` de baixo.

### Dimensionamento de posições
- Usa a propriedade base `Volume`. Quando o sinal exige a reversão de uma posição existente, a estratégia adiciona automaticamente o tamanho absoluto da posição atual ao volume da ordem, garantindo uma inversão completa.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `CandleType` | Tipo de vela e prazo usado para detecção. | `1H` período de tempo |
| `CciPeriod` | Comprimento de lookback do Commodity Channel Index. | `49` |
| `AverageBodyPeriod` | Número de velas para a média móvel do tamanho do corpo. | `11` |
| `EntryConfirmationLevel` | Nível CCI absoluto que valida entradas de padrão. | `50` |
| `ExitLevel` | Nível CCI absoluto que aciona saídas de posição. | `80` |

## Notas
- A estratégia processa apenas velas finalizadas e ignora atualizações parciais.
- Nenhuma ordem stop-loss ou take-profit é definida automaticamente; as saídas são puramente baseadas em sinais, como no consultor especialista original.
- Certifique-se de que o instrumento tenha uma etapa de preço configurada porque a tolerância de igualdade da lógica de velas depende das configurações de segurança.
