# Estratégia de Confirmação de Tendência de MA Dupla
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de Confirmação de Tendência de MA Dupla** replica o expert original do MetaTrader que combina uma média móvel exponencial lenta (EMA) com uma média móvel ponderada linearmente rápida (LWMA). O sistema aguarda que ambas as médias móveis se alinhem na mesma direção e usa o fechamento do candle anterior como confirmação adicional antes de entrar em uma posição. A ideia é participar apenas de fortes oscilações de momentum quando o filtro de tendência lenta e o filtro de confirmação rápida simultaneamente se inclinam para cima ou para baixo.

A implementação do StockSharp processa apenas candles completamente terminados, rastreia a inclinação de cada média móvel durante as últimas três barras e gerencia automaticamente as ordens protetoras via mecanismo integrado `StartProtection`. A estratégia é agnóstica ao instrumento: pode operar em qualquer título e timeframe que forneçam candles e suportem o conceito de "pontos" pelo passo de preço do instrumento.

## Indicadores
- **EMA lenta** – Período padrão 57. Representa a direção de tendência dominante. A estratégia requer que a EMA aumente (ou diminua) por dois candles consecutivos antes de operar.
- **LWMA rápida** – Período padrão 3. Atua como filtro de confirmação de momentum. Sua inclinação deve concordar com a EMA lenta, reforçando que o momentum apoia a tendência.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|---------|-----------|
| `SlowMaLength` | 57 | Período do filtro de tendência EMA lenta. |
| `FastMaLength` | 3 | Período do filtro de confirmação LWMA rápida. |
| `StopLossPoints` | 100 | Distância de stop protetor expressa em pontos do instrumento (multiplicado por `Security.PriceStep`). |
| `TakeProfitPoints` | 100 | Distância de take-profit expressa em pontos do instrumento (multiplicado por `Security.PriceStep`). |
| `CandleType` | Timeframe de 15 minutos | Tipo de dados de candle usado para todos os cálculos. |

Todos os parâmetros são expostos como valores `StrategyParam<T>` para que possam ser modificados em tempo de execução ou otimizados através das ferramentas de otimização do StockSharp.

## Regras de trading
### Setup comprado
1. EMA lenta está subindo: valor atual > valor anterior > valor há dois candles.
2. LWMA rápida está subindo: valor atual > valor anterior > valor há dois candles.
3. Fechamento do candle anterior está acima do valor anterior da EMA lenta.
4. Valor atual da EMA lenta está acima do valor atual da LWMA rápida.
5. Posição atual é plana ou vendida.
6. Quando todas as condições são atendidas, a estratégia envia uma ordem de compra de mercado por `Volume + |Position|` para voltar a uma posição comprada.

### Setup vendido
1. EMA lenta está caindo: valor atual < valor anterior < valor há dois candles.
2. LWMA rápida está caindo: valor atual < valor anterior < valor há dois candles.
3. Fechamento do candle anterior está abaixo do valor anterior da EMA lenta.
4. Valor atual da EMA lenta está abaixo do valor atual da LWMA rápida.
5. Posição atual é plana ou comprada.
6. Quando todas as condições são atendidas, a estratégia envia uma ordem de venda de mercado por `Volume + |Position|` para voltar a uma posição vendida.

### Lógica protetora
- `StartProtection` converte `StopLossPoints` e `TakeProfitPoints` em deslocamentos de preço absoluto multiplicando-os com `Security.PriceStep`. Ordens de stop-loss e take-profit são emitidas como saídas de mercado para que o motor possa fechar a posição mesmo se ordens limite não forem suportadas.
- Quando o sinal oposto aparece, a estratégia reverte imediatamente a posição independentemente das ordens protetoras.

## Detalhes de implementação
- Apenas candles terminados são processados, emulando a verificação de nova barra da versão MQL original.
- A estratégia mantém os últimos dois valores de média móvel e o preço de fechamento anterior em campos privados para evitar pesquisas no histórico do indicador.
- `IsFormedAndOnlineAndAllowTrading()` garante que o trading ocorra apenas quando todos os fluxos de dados estejam ativos e o trading seja permitido.
- Logs de direção de trade (`LogInfo`) fornecem transparência para depuração e monitoramento ao vivo.
- A renderização de gráficos (se disponível) desenha os candles e ambas as médias móveis para validação visual rápida.

## Notas de uso
- Escolha `Volume` de acordo com o tamanho do lote do instrumento. A estratégia sempre envia ordens de mercado de tamanho `Volume + |Position|` para reverter eficientemente.
- Ao executar em instrumentos sem um `PriceStep` definido, o código recorre a um valor de `1`. Ajuste os parâmetros de acordo se o tamanho do tick diferir.
- A otimização pode focar nos períodos de média móvel e distâncias protetoras para adaptar a estratégia a diferentes mercados.
- Combine com filtros adicionais (volatilidade, horários de sessão, etc.) se necessário. A estrutura modular torna fácil a extensão.

## Faixas de otimização sugeridas
- `SlowMaLength`: 20 – 120 com passo 5–10.
- `FastMaLength`: 2 – 10 com passo 1.
- `StopLossPoints` / `TakeProfitPoints`: 50 – 200 dependendo da volatilidade do instrumento.

Essas faixas refletem de perto as configurações originais do expert enquanto fornecem flexibilidade para outros instrumentos.
