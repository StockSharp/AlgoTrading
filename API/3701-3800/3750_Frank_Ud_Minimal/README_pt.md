# Estratégia Mínima de Frank Ud
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este exemplo transporta o consultor especialista clássico **Frank Ud** MetaTrader para StockSharp usando a estratégia de alto nível API. O script MQL original executa uma grade martingale protegida que continua adicionando posições sempre que o preço se move em relação à entrada mais recente. Os lucros são bloqueados quando a ordem mais recente (e, portanto, maior) ganha um número fixo de pips, após o qual *todas* as negociações desse lado são fechadas simultaneamente.

## Lógica central

1. **Hedge simétrico.** A estratégia mantém duas escadas independentes de posições de mercado: uma escada longa e uma escada curta. Portanto, é possível manter posições longas e curtas ao mesmo tempo, como no modo de hedge de MetaTrader.
2. **Martingale progressão.** A primeira ordem de qualquer lado usa `InitialVolume` (padrão 0,1 lote). Cada entrada subsequente no mesmo lado duplica o maior volume atualmente aberto. Todo lote que a estratégia envia — inclusive o primeiro — é então ajustado ao que o instrumento realmente aceita: arredondado para baixo até um número inteiro de unidades de `VolumeStep`, elevado a `MinVolume` se ficar abaixo dele e limitado a `MaxVolume`. As restrições que o instrumento não informa são ignoradas.
3. **Espaçamento de entrada.** Uma nova posição é adicionada somente quando o preço se moveu pelo menos `ReEntryPips` (padrão 41 pips) além do melhor preço de entrada da escada existente. A escada longa espera que os preços de venda caiam abaixo de `lowest_buy - ReEntryPips`, enquanto a escada curta espera que os preços de compra subam acima de `highest_sell + ReEntryPips`. Ambos os lados da cotação vêm do fechamento da mesma vela, de modo que nesta portabilidade as duas comparações são feitas contra o mesmo preço.
4. **Coleta de lucros.** Para cada escada, a negociação com o maior volume atua como a ordem de "gatilho". Quando seu lucro excede `TakeProfitPips` (padrão 65 pips), ou quando o preço atinge o alvo com folga situado a `TakeProfitPips + ExtraTakeProfitPips` pips dessa entrada, cada posição desse lado é achatada com uma única ordem de mercado e a escada é esvaziada.
5. **Proteção de margem.** Antes de enviar uma nova entrada, a estratégia verifica se a margem livre da carteira — seu valor atual menos a comissão que ela informa — permanece acima de `Balance × MinimumFreeMarginRatio` (padrão 0,5). A proteção vale para as duas escadas e para cada entrada delas, inclusive a primeira. Definir a proporção como zero a desativa, e o mesmo acontece se a carteira não devolver valor algum: em ambos os casos a verificação simplesmente passa e a estratégia volta ao comportamento de volume fixo do especialista original.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `TakeProfitPips` | Limite de lucro do pip medido no maior e mais recente pedido. Uma vez ultrapassado, todas as posições desse lado serão fechadas. |
| `ReEntryPips` | Distância mínima de pip entre a melhor entrada existente e a oferta/venda atual antes de uma nova ordem de martingale ser adicionada. |
| `InitialVolume` | Tamanho base do lote para a primeira ordem de cada escada. Os pedidos subsequentes dobram o maior volume ativo. |
| `MinimumFreeMarginRatio` | Proporção necessária entre margem livre e saldo antes que novas entradas sejam permitidas. Defina como 0 para desativar a verificação. Padrão 0,5. |
| `ExtraTakeProfitPips` | Distância adicional em pips somada a `TakeProfitPips` no cálculo do alvo de saída com folga. Padrão 25. |
| `CandleType` | Série de velas que a estratégia assina. Padrão: período de 1 minuto. |

## Notas de implementação

- Um pip não é o passo de preço bruto. Na primeira vela fechada que processa, a estratégia define um pip como um décimo de milésimo do preço cotado, limita-o inferiormente ao passo de preço do instrumento (para que nunca seja mais fino do que o instrumento realmente negocia) e mantém esse valor pelo resto da execução, de modo que a grade não se desloque sob si mesma. Isso reproduz a convenção do forex para a qual o especialista foi escrito (0,0001 no EURUSD a 1,10; 0,01 no USDJPY a 150) e mantém as distâncias significativas em um instrumento cotado com cinco algarismos, onde o passo bruto de 0,01 atingiria um alvo de 65 pips em quase toda vela. Se o instrumento não informar um passo de preço, o pip é definido apenas por essa fração.
- A estratégia é conduzida por velas fechadas, e não por cotações de nível 1. Ela assina a série `CandleType` (por padrão, um período de 1 minuto) e ignora toda vela que ainda não esteja fechada. O histórico incluído não traz livro de ofertas, portanto o fechamento da vela fechada serve tanto como preço de compra quanto como preço de venda. As implementações em C# e em Python assinam exatamente da mesma forma.
- A entrada na escada é registrada no momento em que a ordem é enviada, e não quando ela é executada: na abertura, o fechamento da vela e o volume solicitado são acrescentados à lista; no fechamento, uma única ordem de mercado pelo volume total da escada é enviada e a lista é esvaziada. Nenhum dicionário de intenções de ordem é mantido e nenhum retorno de chamada de execução é usado — neste emulador a execução chega de forma síncrona dentro do registro da ordem, antes mesmo que a ordem pudesse ser escrita em tal dicionário.
- A contabilidade de posição armazena cada entrada da escada (preço e volume) em listas simples em vez de consultar estatísticas cumulativas, preservando o comportamento das matrizes MQL que foram usadas para localizar o maior lote e seu preço de entrada.
- O buffer extra em pips que o especialista original colocou em cada ordem de realização de lucro é exposto como o parâmetro `ExtraTakeProfitPips` (25 pips por padrão) e retido como uma condição de saída adicional.

> As implementações estão disponíveis em C# e Python.
