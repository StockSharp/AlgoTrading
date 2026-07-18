# Estratégia Forex Sky
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Forex Sky Strategy** é uma versão direta do MetaTrader consultor especialista `Forex_SKY.mq4`. Ele negocia MACD oscilações de impulso e limita-se estritamente a uma única posição por dia de negociação. A implementação StockSharp mantém os limites originais MACD e a verificação de segurança que evita mais de um pedido por vela.

A estratégia segue o prazo definido por `CandleType` (velas de 15 minutos por padrão) e avalia o clássico MACD (26/12/9) no fechamento de cada vela concluída.

## Lógica de negociação
- **Entrada comprada** – Faça uma compra no mercado quando:
  - A linha principal MACD atual está acima de zero;
  - Também excede `+0.00009` para confirmar o impulso;
  - Pelo menos uma das três leituras anteriores de MACD foi menor ou igual a zero (capturando uma mudança de alta do território negativo).
- **Entrada a descoberto** – Faça uma venda no mercado quando uma das seguintes situações for verdadeira:
  - A linha principal MACD está abaixo de zero, cai abaixo de `-0.0004`, pelo menos uma das últimas três leituras não foi negativa e o valor de quatro barras atrás foi de pelo menos `+0.001`.
  - **Ou** o valor de quatro barras atrás era `≥ +0.003`, o que autoriza imediatamente uma negociação a descoberto, assim como no código MetaTrader original.
- **Gerenciamento de posição** – O algoritmo nunca abre mais de uma ordem por vela (`Time0` guarda) e nunca negocia mais de uma vez por dia corrido (`CheckTodaysOrders` guarda). As ordens de saída protetora são tratadas pelo auxiliar StockSharp `StartProtection`, portanto, todas as paradas e destinos permanecem sincronizados com o volume atual.

Não existe uma lógica de flatting autónoma para além das ordens de proteção – espera-se que as posições sejam fechadas através de take-profit, stop-loss ou intervenção manual, refletindo o comportamento do consultor especialista original.

## Parâmetros
| Nome | Padrão | Descrição |
|------|---------|-------------|
| `FastPeriod` | 12 | Comprimento EMA rápido do indicador MACD. |
| `SlowPeriod` | 26 | Comprimento lento de EMA do indicador MACD. |
| `SignalPeriod` | 9 | Comprimento do sinal EMA do indicador MACD. |
| `TakeProfitPoints` | 100 | Distância até a ordem de take-profit expressa em pontos do instrumento. Convertido em preço multiplicando pela etapa do preço do título. |
| `StopLossPoints` | 3.000 | Distância até a ordem de stop loss em pontos do instrumento. |
| `TradeVolume` | 0,1 | Tamanho base da ordem de mercado (lotes). |
| `CandleType` | Período de 15 minutos | Prazo que alimenta os cálculos de MACD e as decisões de negociação. |

### Cálculo de ponto de instrumento
`TakeProfitPoints` e `StopLossPoints` são especificados exatamente como a versão MetaTrader—`Point` em MQL4 corresponde a `Security.PriceStep` em StockSharp. Para um par forex de cinco dígitos (`PriceStep = 0.00001`), as configurações padrão são traduzidas para:
- Take-profit: `100 × 0.00001 = 0.001` unidades de preço.
- Stop-loss: `3000 × 0.00001 = 0.03` unidades de preço.

## Gestão de risco
`StartProtection` instala automaticamente as ordens de take-profit e stop-loss após o preenchimento de uma entrada. Eles estão vinculados à direção da negociação e usam ordens de mercado quando acionados, correspondendo ao comportamento MetaTrader. Defina qualquer parâmetro como `0` para desativar a ordem de proteção correspondente.

## Notas de migração
- O buffer de histórico MACD mantém os últimos quatro valores concluídos nos campos de classe, portanto, nenhuma chamada de indicador com índices alterados é necessária.
- A limitação diária da negociação e a restrição de negociação única por barra replicam `CheckTodaysOrders()` e `Time0` da fonte original.
- Todos os comentários foram reescritos em inglês e a lógica depende de StockSharp ligações de alto nível (`Bind`) para processamento de indicadores.

## Dicas de uso
- Ajuste `CandleType` para o período do gráfico que deseja emular; o script original herda o período do gráfico automaticamente.
- Como apenas uma negociação é permitida por dia, escolha mercados com oscilações intradiárias significativas ou considere aumentar os limites de MACD ao usar instrumentos de volatilidade mais alta.
- Monitore o relógio/fuso horário da plataforma para garantir que o limite do dia corresponda à sua sessão de negociação, à medida que o contador de limite é zerado com base na data de abertura da vela.
