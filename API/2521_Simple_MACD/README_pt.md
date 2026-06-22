# Simple MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Simple MACD replica a lógica do assessor MQL5 `Simple_MACD.mq5` no StockSharp. A estratégia segue a inclinação da linha principal do MACD calculada em velas completadas e continua adicionando à posição sempre que a inclinação permanecer na mesma direção.

## Visão Geral

- **Mercado**: qualquer instrumento com dados de velas e horários de trading contínuos.
- **Indicador principal**: Convergência/Divergência de Médias Móveis (MACD) usando médias móveis exponenciais 12/26 e sinal 9.
- **Abordagem**: seguimento de momentum. A estratégia compara as duas leituras de MACD completadas mais recentes e vai comprado quando a linha sobe, ou vendido quando a linha cai.
- **Tipo de ordem**: apenas ordens de mercado. Cada sinal agrega a quantidade necessária para fechar a posição oposta e adiciona o volume de trading configurado em cima, espelhando o assessor especialista original.

## Notas de Conversão

- O bot MQL5 acionava uma vez por nova barra comparando `MACD(1)` e `MACD(2)` (as duas barras completadas anteriores). No StockSharp, a mesma comparação é executada quando uma vela termina, antes da próxima barra começar.
- A versão MQL dependia de enumeração explícita de posições e verificações manuais de volume. A versão StockSharp agrega o volume automaticamente com chamadas `BuyMarket`/`SellMarket` e o parâmetro `TradeVolume` da estratégia.
- As verificações de hedge do código MQL não são necessárias porque o StockSharp rastreia a posição líquida diretamente.

## Regras de Trading

### Entrada e Escalonamento

1. Calcular a linha principal do MACD em cada vela terminada.
2. Armazenar os dois últimos valores do MACD e compará-los:
   - Se `MACD(1) > MACD(2)`, a inclinação é de alta. A estratégia compra um volume igual a `TradeVolume + max(0, -Posição)` para fechar vendidos e adicionar novos comprados.
   - Se `MACD(1) < MACD(2)`, a inclinação é de baixa. A estratégia vende `TradeVolume + max(0, Posição)` para fechar comprados e adicionar novos vendidos.
3. Se ambos os valores são iguais, nenhuma nova ordem é enviada.

### Gestão de Posições

- A estratégia continua empilhando ordens na direção atual enquanto a inclinação do MACD não mudar de sinal, assim como o assessor original que enviava uma compra ou venda em cada barra qualificada.
- Sinais opostos eliminam qualquer exposição aberta antes de construir a nova posição.
- Não há níveis de stop-loss ou take profit incorporados; o controle de risco depende de regras externas de gestão de dinheiro ou supervisão manual.

### Salvaguardas Adicionais

- O trading é ignorado até que o indicador MACD esteja completamente formado.
- Apenas velas completadas (`CandleStates.Finished`) são processadas, prevenindo ações prematuras em dados parciais.
- As mensagens de log rastreiam cada negociação e mostram os dois valores do MACD usados para tomar a decisão para uma análise de backtesting mais fácil.

## Parâmetros

| Parâmetro | Valor padrão | Descrição |
|-----------|--------------|-----------|
| `FastPeriod` | 12 | Comprimento da EMA rápida para o cálculo do MACD. |
| `SlowPeriod` | 26 | Comprimento da EMA lenta para o cálculo do MACD. |
| `SignalPeriod` | 9 | Período da EMA de sinal retido por compatibilidade com as configurações originais. |
| `TradeVolume` | 0.1 | Volume adicionado em cada sinal antes de contabilizar a reversão de posição. |
| `CandleType` | Período de 1 minuto | Tipo de vela usado para alimentar o indicador. Ajustável para qualquer período desejado. |

Todos os parâmetros são expostos como parâmetros de estratégia e marcados como otimizáveis onde relevante.

## Visualização

- A estratégia cria automaticamente uma área de gráfico (quando disponível) com as velas de preço e sobrepõe a saída do indicador MACD.
- As negociações próprias são desenhadas no gráfico para mostrar com que frequência a estratégia escala posições em condições de tendência.

## Uso Recomendado

- Aplicar em instrumentos com tendência onde o momentum persiste por várias barras; mercados em range causarão reversões frequentes e negociações whipsaw.
- Combinar com gestão de risco em nível de portfólio já que a lógica base não tem mecanismo de stop intrínseco.
- Considerar otimizar o `TradeVolume` e os períodos do MACD para o instrumento e período alvo.

## Arquivos

- `CS/SimpleMacdStrategy.cs` – implementação em StockSharp da lógica da estratégia.
- `README.md`, `README_ru.md`, `README_zh.md` – documentação detalhada em três idiomas.
