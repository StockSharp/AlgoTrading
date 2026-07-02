# Parabolic SAR Estratégia do Bug 3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Parabolic SAR Estratégia Bug 3** é uma StockSharp versão de alto nível do MetaTrader 4 consultor especialista `pSAR_bug_3.mq4` localizado em `MQL/9786`. O robô reage ao primeiro Parabolic SAR ponto que aparece no lado oposto do preço. Quando o SAR cai abaixo do fechamento da vela, a estratégia abre uma posição longa após fechar qualquer exposição curta. Quando o SAR salta acima do fechamento, ele reverte para uma posição curta. Cada negociação é protegida por níveis fixos de stop-loss e take-profit medidos em Parabolic SAR pontos e escalonados pelo mesmo multiplicador do programa MQL original.

## Lógica de negociação
1. **Dados e indicadores de mercado** – a estratégia assina um tipo de vela configurável (período de 15 minutos por padrão) e vincula um indicador Parabolic SAR com etapa de aceleração especificada pelo usuário e aceleração máxima.
2. **Acompanhamento de estado** – após a primeira vela concluída, o código armazena se o valor Parabolic SAR está acima ou abaixo do fechamento. As próximas velas comparam o novo estado com o anterior para detectar a mudança do indicador.
3. **Entradas longas** – se o Parabolic SAR mudar de cima para baixo, a estratégia envia uma ordem de mercado dimensionada para fechar qualquer posição curta ativa e abrir o volume comprado configurado. Os preços protetores de stop-loss e take-profit são calculados imediatamente após a entrada.
4. **Entradas curtas** – quando o Parabolic SAR cruza de baixo para cima, o código reflete o comportamento das negociações curtas: ele nivela as posições longas e abre uma ordem curta.
5. **Saídas** – em cada vela finalizada, os preços máximos e mínimos são comparados com os níveis de proteção armazenados. A violação do stop-loss ou do take-profit aciona uma ordem de mercado que fecha a posição aberta, correspondendo à abordagem MetaTrader das ordens de proteção do lado do corretor.

## Gestão de risco
- As distâncias stop-loss e take-profit são convertidas multiplicando `StopLossPoints` ou `TakeProfitPoints` por `StopMultiplier` e o instrumento `PriceStep` (ou `0.0001` se o símbolo não fornecer um passo).
- As ordens de mercado só são enviadas quando `IsFormedAndOnlineAndAllowTrading()` confirma que a assinatura está ativa e a negociação é permitida.
- Sempre que a direção da posição muda, os níveis de proteção não utilizados do lado antigo são apagados para evitar saídas obsoletas.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `TradeVolume` | `0.1` | Volume do pedido em lotes. A atualização do valor também altera a propriedade base `Strategy.Volume`. |
| `StopLossPoints` | `90` | Distância de stop-loss expressa em Parabolic SAR pontos, posteriormente escalonada por `StopMultiplier` e a etapa de preço do instrumento. |
| `TakeProfitPoints` | `20` | Distância de lucro expressa em Parabolic SAR pontos, posteriormente escalonada em `StopMultiplier` e a etapa de preço. |
| `StopMultiplier` | `10` | Multiplicador que reproduz a entrada MetaTrader `StopMult`, permitindo compatibilidade de corretores de pip fracionário. |
| `SarStep` | `0.02` | Fator de aceleração inicial para o indicador Parabolic SAR. |
| `SarMaximum` | `0.2` | Fator máximo de aceleração para o indicador Parabolic SAR. |
| `CandleType` | `15m time-frame` | Tipo de vela usado para cálculos de indicadores e detecção de sinal. |

## Notas de conversão
- MetaTrader posições fechadas antes de abrir a negociação oposta usando ordens separadas. A versão StockSharp alcança o mesmo resultado enviando uma única ordem de mercado dimensionada para compensar qualquer exposição oposta e estabelecer o novo volume de posição.
- As ordens de stop-loss e take-profit do lado do corretor são emuladas monitorando os extremos das velas e enviando saídas de mercado assim que os limites forem violados.
- O parâmetro adicional `StopMultiplier` aceita qualquer valor positivo, mas o padrão é `10`, o único multiplicador documentado nos comentários do código original.
- Nenhuma versão do Python é fornecida para esta conversão, exatamente conforme solicitado na descrição da tarefa.
