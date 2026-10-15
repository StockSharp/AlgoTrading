# Estratégia de direção Nevalyashka
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Nevalyashka é uma porta C# do MetaTrader 4 consultor especialista original `Nevalyashka.mq4`. O EA inverte repetidamente sua direção de negociação: ele abre uma única ordem de mercado, espera até que a posição seja fechada por um stop-loss, take-profit ou ação manual e entra instantaneamente novamente na direção oposta com o mesmo volume. A implementação StockSharp reproduz esse comportamento enquanto expõe todas as configurações críticas como parâmetros de estratégia.

## Lógica de negociação
1. **Inicialização**
   - Quando a estratégia é iniciada, ela calcula o tamanho do pip a partir do `PriceStep` do instrumento. Para símbolos Forex de 3 e 5 dígitos, o passo é multiplicado por 10 para corresponder à definição de ponto MetaTrader.
   - `StartProtection` é configurado com distâncias de stop-loss e take-profit convertidas de pips em pontos de preço. Ordens de proteção são anexadas a cada posição subsequente.
   - Uma ordem de mercado inicial é enviada na direção definida por `InitialDirection` (padrão: short). O volume solicitado é arredondado para o lote válido mais próximo usando os valores `VolumeStep`, `MinVolume` e `MaxVolume` do título.

2. **Rastreamento de posição**
   - `OnPositionChanged` captura todas as alterações na exposição líquida. Quando uma nova posição é aberta, a estratégia armazena o volume preenchido e lembra o lado comercial.
   - Assim que a posição retornar totalmente à estabilidade, a estratégia emite imediatamente uma nova ordem de mercado na direção oposta, reutilizando o tamanho do lote armazenado anteriormente.

3. **Tratamento de falhas**
   - Se a corretora rejeitar o registro de uma ordem, o sinalizador de direção pendente será apagado, permitindo que o operador da plataforma tente novamente manualmente ou ajuste os parâmetros sem estado interno obsoleto.

O fluxo de trabalho resultante reflete a ideia “rechonchuda” do script original: o bot está sempre no mercado, alternando entre posições longas e curtas com saídas fixas.

## Parâmetros
| Nome | Descrição | Padrão | Notas |
| --- | --- | --- | --- |
| `StopLossPips` | Distância do batente de proteção em pips. | `50` | Convertido em preços por meio do cálculo do tamanho do pip; defina como `0` para desativar a parada. |
| `TakeProfitPips` | Distância do take-profit protetor em pips. | `50` | Convertido da mesma forma que o stop loss; definido como `0` para desativar o take-profit. |
| `Volume` | Tamanho do lote usado para a primeira negociação. | `1` | Após o primeiro preenchimento, a estratégia reutiliza o volume efetivamente executado para todas as entradas futuras. |
| `InitialDirection` | Lado da ordem de mercado inicial. | `Sell` | Escolha entre `Buy` e `Sell` para corresponder ao viés inicial desejado. |

## Notas de implementação
- Não são necessárias assinaturas de velas ou indicadores; a estratégia reage apenas a eventos de posição e confirmações de pedidos.
- `IsFormedAndOnlineAndAllowTrading()` é consultado antes de cada entrada para garantir que o conector esteja pronto para negociação.
- O arredondamento de volume usa `MidpointRounding.AwayFromZero` para que os lotes fracionários sempre cheguem a um nível negociável em vez de zero.
- A lógica de conversão de pip depende de metadados de instrumentos em vez de suposições codificadas, o que faz com que a porta funcione em símbolos FX, CFD ou futuros com diferentes formatos de preços.

## Diferenças versus a versão MQL
- A variante StockSharp expõe a direção inicial como um parâmetro em vez de forçar o short inicial do script MT4.
- As ordens stop-loss e take-profit são gerenciadas por meio de `StartProtection`, que produz ordens de proteção nativas compatíveis com qualquer conector StockSharp.
- As rejeições de pedidos limpam o estado pendente interno para evitar o envio repetido de solicitações inválidas.

Esses ajustes mantêm o espírito do consultor original enquanto se integram perfeitamente ao StockSharp API de alto nível.
