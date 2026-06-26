# Estratégia Tunnel Method
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia Tunnel Method é um port do StockSharp do consultor especialista "Tunnel Method" publicado originalmente para MetaTrader 5. Ele usa três médias móveis simples (SMA) deslocadas para detectar rompimentos direcionais. A média rápida deve perfurar um "túnel" de preço criado pelas médias lenta e média com um recuo configurável para confirmar uma negociação. A estratégia inclui regras de gerenciamento de posição idênticas à versão MQL, incluindo níveis fixos de stop-loss e take-profit baseados em pips, um trailing stop que bloqueia lucros com um filtro de passo e um tempo de espera mínimo entre avaliações de entrada.

## Lógica da estratégia

- **Indicadores**: três médias móveis simples no mesmo instrumento e período.
  - *Primeira SMA* (linha lenta): período longo com deslocamento zero. Define o limite inferior do túnel altista e o limite superior do túnel baixista.
  - *Segunda SMA* (linha média): período médio com deslocamento positivo. É usada principalmente para sinais vendidos, criando uma barreira projetada para frente.
  - *Terceira SMA* (linha rápida): período curto com o maior deslocamento positivo. Os rompimentos desta linha pelo túnel acionam ordens.
- **Recuo**: as médias móveis devem estar separadas por pelo menos `IndentPips` (convertidos para unidades de preço) para evitar condições voláteis. A média rápida deve cruzar de baixo para cima a média lenta mais metade do recuo para abrir posições compradas, e cruzar de cima para baixo a média média menos metade do recuo para abrir posições vendidas.
- **Cadência de entrada**: um novo sinal é avaliado apenas quando `PauseSeconds` tiverem passado desde a avaliação anterior. Isso espelha o EA original, que limita o processamento de OnTick para reduzir o ruído.
- **Modo de posição única**: a estratégia mantém apenas uma posição de cada vez. Uma nova ordem é ignorada se outra posição já estiver aberta.

## Gestão de risco

- **Stop Loss**: distância fixa opcional abaixo (para posições compradas) ou acima (para posições vendidas) do preço de entrada, medida em pips via `StopLossPips`.
- **Take Profit**: alvo fixo opcional em pips via `TakeProfitPips`.
- **Trailing Stop**: habilitado quando tanto `TrailingStopPips` quanto `TrailingStepSteps` são positivos. Uma vez que o preço se move a favor da negociação por `TrailingStopPips + TrailingStepPips`, o stop é puxado para `TrailingStopPips` atrás do fechamento atual. O trailing stop atualiza apenas quando o preço avança pelo menos o passo de trailing, evitando ajustes excessivamente frequentes.
- **Saída de posição**: a estratégia fecha posições ao mercado quando stops, take profits ou níveis de trailing são violados. Isso replica como o EA original reagiria após o broker executar ordens de proteção.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| `TradeVolume` | 1 | Volume de ordem por negociação. |
| `StopLossPips` | 50 | Distância de stop-loss em pips. Use `0` para desabilitar. |
| `TakeProfitPips` | 50 | Distância de take-profit em pips. Use `0` para desabilitar. |
| `TrailingStopPips` | 5 | Distância de trailing base em pips. Requer `TrailingStepPips > 0`. |
| `TrailingStepPips` | 5 | Ganho incremental mínimo antes que o trailing stop possa se mover. |
| `FirstMaPeriod` | 160 | Período da SMA lenta. |
| `FirstMaShift` | 0 | Deslocamento para frente da SMA lenta. |
| `SecondMaPeriod` | 80 | Período da SMA média usada para sinais vendidos. |
| `SecondMaShift` | 1 | Deslocamento para frente da SMA média. |
| `ThirdMaPeriod` | 20 | Período da SMA rápida. |
| `ThirdMaShift` | 2 | Deslocamento para frente da SMA rápida. |
| `IndentPips` | 1 | Lacuna mínima entre médias para validar um rompimento. |
| `PauseSeconds` | 45 | Atraso entre verificações de entrada consecutivas. |
| `CandleType` | Período de 5 minutos | Série de velas usada para cálculos do indicador. |

Todos os parâmetros baseados em pips são automaticamente convertidos para unidades de preço usando o `PriceStep` do instrumento e a precisão decimal, com tratamento especial para símbolos FX de 3 e 5 dígitos como na versão MetaTrader.

## Notas práticas

1. **Configuração do instrumento**: certifique-se de que o `Security` atribuído à estratégia tenha `PriceStep` e `Decimals` corretos. As distâncias de pips convertidas serão imprecisas caso contrário.
2. **Alinhamento de períodos**: o `CandleType` padrão usa velas de 5 minutos, mas você pode alinhá-lo com o período usado no MetaTrader (por exemplo M1) mudando o parâmetro.
3. **Tratamento de volume**: `TradeVolume` define o tamanho total por entrada. A estratégia fecha posições com ordens de mercado simétricas para que o tamanho da posição permaneça consistente.
4. **Requisitos de trailing**: o construtor aplica a regra do EA original: se `TrailingStopPips` é positivo enquanto `TrailingStepPips` é zero, a estratégia lança um erro de inicialização para evitar configurações inconsistentes.
5. **Otimização**: o design de parâmetros segue as convenções do StockSharp. Cada parâmetro pode ser otimizado ou vinculado a controles de UI no Designer, facilitando o ajuste de períodos, recuo ou valores de trailing.

## Arquivos

- `CS/TunnelMethodStrategy.cs` – implementação principal da estratégia.
- `README.md` – documentação em inglês (este arquivo).
- `README_ru.md` – documentação em russo.
- `README_zh.md` – documentação em chinês.

A tradução para Python é intencionalmente omitida, coincidindo com a solicitação de entregar apenas a versão C# nesta etapa.
