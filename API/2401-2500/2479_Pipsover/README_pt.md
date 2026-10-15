# Estratégia Pipsover
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Pipsover é uma estratégia de reversão de momentum que reage a extremos fortes do oscilador Chaikin. O Expert Advisor original do MetaTrader 5 abre uma nova operação quando o oscilador imprime um pico pronunciado enquanto a vela anterior recua para a média móvel simples de 20 períodos. O porte em C# mantém a mesma ideia reconstruindo o oscilador Chaikin com a linha de acumulação/distribuição e dois médias móveis exponenciais. Cada operação é protegida com as mesmas distâncias de stop-loss e take-profit definidas no script para que o controle de risco corresponda à implementação de referência.

## Indicadores e ferramentas
- **Média Móvel Simples (SMA 20)** – fornece a âncora de reversão à média. A estratégia requer que a vela anterior toque ou cruze a média antes de se tornar elegível para uma operação.
- **Oscilador Chaikin (EMA 3 – EMA 10 de ADL)** – mede a pressão entre preço e volume. Leituras extremamente negativas acionam oportunidades de compra e valores positivos extremos acionam oportunidades de venda.
- **Linha de Acumulação/Distribuição (ADL)** – alimenta o oscilador Chaikin. As EMAs rápida e lenta rodam sobre este fluxo de valores para imitar o indicador `iChaikin` do MQL5.

## Lógica de negociação
### Entrada comprada
1. Aguardar uma vela completada para que todos os valores de indicadores sejam finais.
2. Verificar que a vela anterior fechou com alta (`Close > Open`).
3. Confirmar que a mínima anterior mergulhou abaixo da SMA20, sinalizando um recuo.
4. Ler o valor do oscilador Chaikin da barra anterior. Deve ser menor que `-OpenLevel` para refletir um pico de sobrecompra negativa.
5. Quando todas as condições são atendidas e nenhuma posição está atualmente aberta, enviar uma ordem de compra a mercado.

### Entrada vendida
1. Aguardar uma vela completada.
2. Verificar que a vela anterior fechou com baixa (`Close < Open`).
3. Confirmar que a máxima anterior ultrapassou a SMA20.
4. Garantir que o oscilador Chaikin na barra anterior seja maior que `OpenLevel`.
5. Se não houver posição ativa, colocar uma ordem de venda a mercado.

### Lógica de saída
- As **posições compradas** fecham quando a próxima vela após a entrada mostra uma estrutura baixista (fechamento abaixo da abertura), sua máxima permanece acima da SMA20 e o oscilador Chaikin sobe acima de `CloseLevel`.
- As **posições vendidas** fecham quando a próxima vela mostra uma estrutura altista, sua mínima desce abaixo da SMA20 e o oscilador Chaikin cai abaixo de `-CloseLevel`.
- As saídas de proteção monitoram cada vela terminada. Uma posição comprada fecha se o preço negocia em ou abaixo do stop-loss calculado ou em ou acima do take-profit calculado. Para vendas a comparação é invertida.

## Gerenciamento de posição
- Apenas uma posição líquida é permitida a qualquer momento. Ordens pendentes são canceladas antes de abrir uma nova operação para replicar o comportamento de posição única do MQL5.
- Os valores de stop-loss e take-profit são calculados a partir do passo de preço do instrumento. Para compradas, o stop é definido `StopLossPoints * PriceStep` abaixo do preço de execução e o take-profit `TakeProfitPoints * PriceStep` acima. Vendas usam distâncias simétricas mas invertidas.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `TradeVolume` | 0.1 | Tamanho da ordem usado para cada ordem a mercado. |
| `MaLength` | 20 | Período da SMA de recuo. |
| `StopLossPoints` | 65 | Offset de stop-loss em passos de preço desde a entrada. |
| `TakeProfitPoints` | 100 | Offset de take-profit em passos de preço desde a entrada. |
| `OpenLevel` | 100 | Limiar absoluto de Chaikin que habilita novas entradas. |
| `CloseLevel` | 125 | Limiar absoluto de Chaikin que força a saída da posição. |
| `ChaikinFastLength` | 3 | Comprimento da EMA rápida do oscilador Chaikin. |
| `ChaikinSlowLength` | 10 | Comprimento da EMA lenta do oscilador Chaikin. |
| `CandleType` | 1 hora | Período usado para a assinatura de velas; ajustar para corresponder à sessão de negociação de interesse. |

## Notas de implementação
- A estratégia vincula a linha de acumulação/distribuição e a SMA ao feed de velas através de `SubscribeCandles().Bind(...)`, garantindo que os valores dos indicadores cheguem já sincronizados com cada vela terminada.
- Os valores de Chaikin são reconstruídos manualmente dentro de `ProcessCandle` para evitar o acesso de baixo nível a buffers proibido pelas diretrizes de conversão.
- O algoritmo armazena a última vela completada, o valor da SMA e a leitura de Chaikin para reproduzir a lógica `shift=1` (`iClose(...,1)`, `iLow(...,1)`, `iChaikin(...,1)`) usada no script MQL5.
- Os níveis de alvo de proteção são rastreados dentro da classe de estratégia em vez de depender de stops gerenciados pelo broker, então o comportamento é consistente entre simulações e negociação ao vivo.
