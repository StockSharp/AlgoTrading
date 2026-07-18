# Parabolic SAR Limite
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Parabolic SAR Limit é uma porta direta do consultor especialista MT4 **ytg_Parabolic_exp.mq4**. O sistema mantém continuamente as ordens com limite de compra e venda coladas ao valor Parabolic SAR e permite que o mercado coloque a ordem em uma negociação. Uma vez preenchida, a estratégia supervisiona a posição aberta e realiza saídas de stop-loss ou take-profit usando extremos de velas, refletindo o comportamento original MQL.

## Lógica da estratégia

1. A estratégia assina uma série de velas configuráveis (período de 4 horas por padrão) e calcula o indicador Parabolic SAR com o mesmo passo e valores máximos do script MT4.
2. Em cada vela acabada:
   - Se o ponto SAR estiver *abaixo* do mínimo da barra e o melhor lance estiver pelo menos `MinOrderDistancePoints` acima do preço SAR, um pedido com limite de compra será colocado (ou realinhado) exatamente no valor SAR.
   - Se o ponto SAR estiver *acima* da máxima da barra e a melhor venda estiver pelo menos `MinOrderDistancePoints` abaixo do preço SAR, uma ordem de venda com limite é colocada (ou realinhada) a esse preço SAR.
   - Apenas uma ordem pendente por lado é mantida. Quando o SAR se move, a ordem pendente ativa é cancelada e uma nova é enviada no nível atualizado.
3. Quando uma ordem pendente é preenchida, as distâncias de stop-loss e take-profit (expressas em pontos) são convertidas em preços absolutos usando a etapa de preço do título. Esses níveis são armazenados como limites de proteção virtuais.
4. Cada nova vela verifica os limites registrados. Se o intervalo da vela tocar o nível de stop ou take, a estratégia fecha a posição correspondente imediatamente e redefine o estado de proteção.

## Parâmetros

- **CandleType** – prazo para velas de sinal. O padrão é velas de 4 horas para corresponder ao parâmetro de entrada MT4 `timeframe`.
- **SarStep** – Parabolic SAR fator de aceleração (`step` em MT4). Controla a rapidez com que o SAR alcança o preço.
- **SarMaximum** – aceleração máxima (`maximum` em MT4). Limita a velocidade SAR.
- **StopLossPoints** – distância em pontos entre o preço de entrada e o nível de stop. Defina como `0` para desativar.
- **TakeProfitPoints** – distância em pontos entre o preço de entrada e o nível de take-profit. Defina como `0` para desativar.
- **MinOrderDistancePoints** – imita `MODE_STOPLEVEL` no MT4. As ordens pendentes são enviadas apenas se o preço de mercado estiver mais distante do que esta distância do valor SAR.
- **OrderVolume** – lotes (volume) para cada ordem pendente. Alinhe-o com o `VolumeStep` do instrumento.

Todas as distâncias baseadas em pontos são convertidas em preços usando o instrumento `PriceStep`, para que o comportamento permaneça consistente em todos os mercados.

## Comportamento de Negociação

- Funciona em ambas as direções simultaneamente: uma ordem com limite de compra e uma ordem de venda podem coexistir se o preço SAR mudar.
- Os pedidos pendentes são sempre alinhados à leitura mais recente de SAR; pedidos obsoletos são cancelados antes que um novo seja registrado.
- As saídas de stop-loss e take-profit são tratadas virtualmente por meio de altos e baixos de velas, porque estratégias StockSharp de alto nível não vinculam SL/TP diretamente a pedidos pendentes.
- A estratégia depende dos melhores dados de compra/venda, quando disponíveis; caso contrário, o preço de fechamento da vela será usado como alternativa para avaliar as condições de distância.

## Portando Notas

- O padrão de `MinOrderDistancePoints` é `0`, mas você pode configurá-lo para o nível de stop da corretora se a plataforma de negociação impor uma distância mínima.
- Os níveis de proteção são zerados automaticamente quando a posição é fechada ou quando a ordem pendente é cancelada, mantendo a lógica idêntica à do especialista MT4.
- Os comentários dentro do código C# explicam o uso de API de alto nível, a vinculação do indicador e o ciclo de vida do pedido para facilitar a manutenção.

## Dicas de uso

- Fornece cotações de nível 1 para verificação precisa de distância; caso contrário, certifique-se de que o preço de fechamento da vela seja um bom indicador do preço de mercado atual.
- Revise `PriceStep` e `VolumeStep` do seu símbolo para que as distâncias dos pontos e o volume do pedido sejam convertidos em preços e quantidades válidos.
- Como as saídas são avaliadas em velas concluídas, considere usar prazos mais curtos se precisar de granularidade mais refinada para monitoramento de stop-loss.
