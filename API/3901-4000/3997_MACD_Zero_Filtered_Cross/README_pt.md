# MACD Cruzamento filtrado zero
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
MACD Zero Filtered Cross é uma porta C# do MetaTrader 4 consultor especialista `Robot_MACD_12.26.9`. O robô original observa
cruzamentos entre a linha MACD e sua linha de sinal, mas filtra novas negociações para que entradas longas ocorram apenas enquanto ambas as linhas
permanecem abaixo do eixo zero e entradas curtas ocorrem apenas enquanto ambas as linhas permanecem acima do eixo. A versão StockSharp mantém o
mesma lógica cruzada, adiciona controles de risco que se integram à estrutura (filtragem de saldo de portfólio e take-profit unificado
gerenciamento) e expõe todos os valores configuráveis por meio de parâmetros estratégicos que suportam a otimização.

A estratégia depende de velas finalizadas em um período configurável. Os valores dos indicadores são fornecidos pelo built-in
Indicador `MovingAverageConvergenceDivergenceSignal`, garantindo que a estratégia permaneça compatível com o API de alto nível e
respeita as diretrizes de uso de `BindEx`.

## Lógica estratégica
### Cálculo do indicador
* **MACD linha** – diferença entre uma média móvel exponencial rápida e lenta (comprimentos padrão: 12 e 26).
* **Linha de sinal** – média móvel exponencial aplicada à linha MACD (comprimento padrão: 9).
* **Filtro zero** – o sinal de ambas as linhas em relação a zero determina se um cruzamento pode acionar uma entrada de posição.

### Regras de entrada
* **Configuração longa**
  * A linha MACD deve cruzar acima da linha de sinal (`MACD[t-1] < Signal[t-1]` e `MACD[t] > Signal[t]`).
  * Tanto a linha MACD quanto a linha de sinal devem estar abaixo de zero após o cruzamento.
  * A posição líquida atual deve ser plana ou curta; as posições curtas existentes são fechadas imediatamente antes de tentar uma posição comprada.
  * Um filtro de saldo opcional exige que o valor do portfólio exceda um mínimo configurável antes que um novo pedido seja enviado.
* **Configuração curta**
  * A linha MACD deve cruzar abaixo da linha de sinal (`MACD[t-1] > Signal[t-1]` e `MACD[t] < Signal[t]`).
  * Ambas as linhas indicadoras devem estar acima de zero após o cruzamento.
  * A posição líquida atual deve ser plana ou longa; as posições compradas existentes são achatadas antes que uma nova venda seja enviada.
  * O filtro de saldo é aplicado simetricamente às entradas curtas.

### Regras de saída
* **Saída cruzada** – quando a linha MACD cruza de volta através da linha de sinal contra a posição atual, a estratégia fecha
o comércio aberto no mercado. Isso reflete o EA original, que sempre achatava a posição em um cruzamento oposto antes
em busca de novas oportunidades.
* **Take-profit fixo** – um take-profit baseado em unidade (expresso em faixas de preço) é aplicado via `StartProtection`. O nível corresponde
o parâmetro MQL `TakeProfit` e usa o valor do ponto do instrumento.

### Gestão de risco e capital
* **Manuseio de volume** – o parâmetro `LotVolume` reflete o tamanho do lote MT4. A estratégia envia esse volume exato para cada entrada.
* **Filtro de saldo** – o parâmetro `MinimumBalancePerVolume` multiplica o volume solicitado para determinar o portfólio mínimo
valor necessário antes que novas entradas sejam permitidas. Se a verificação do saldo falhar, a estratégia registra uma mensagem e ignora a negociação,
correspondendo à salvaguarda original de margem livre.
* **Integridade dos dados** – os sinais são processados apenas em velas finalizadas e após `IsFormedAndOnlineAndAllowTrading()` confirmar que
tanto a conexão quanto os indicadores estão prontos.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `FastPeriod` | EMA comprimento do componente rápido MACD. |
| `SlowPeriod` | EMA comprimento do componente lento MACD. |
| `SignalPeriod` | EMA comprimento da linha de sinal MACD. |
| `TakeProfitPoints` | Distância até o take-profit protetor em faixas de preço. Defina como `0` para desativar. |
| `LotVolume` | Volume base do pedido, equivalente à entrada “Lotes” da versão MT4. |
| `MinimumBalancePerVolume` | Valor mínimo da carteira exigido por unidade de volume negociado antes de abrir uma posição. Defina como `0` para ignorar o filtro. |
| `CandleType` | Período usado para construir velas e alimentar a cadeia de indicadores. |

## Notas adicionais
* A estratégia usa a sobrecarga `BindEx` para que o indicador MACD possa fornecer os valores de MACD e de sinal em um único
retorno de chamada sem chamadas manuais para `GetValue`.
* Todos os comentários dentro do código C# são escritos em inglês, correspondendo às diretrizes do projeto.
* Não há tradução em Python para esta estratégia; apenas a implementação C# é fornecida no pacote API.
* Para replicar melhor o comportamento original do MT4, selecione um período de vela que corresponda ao gráfico onde o EA costumava ser executado
e manter o parâmetro de volume consistente com o tamanho do lote negociado anteriormente.
