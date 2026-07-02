# Estratégia ZigZag EvgeTrofi 1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)


## Visão geral
ZigZag EvgeTrofi 1 reproduz o comportamento do consultor especialista MetaTrader original que reage ao último ponto de swing do ZigZag. A estratégia monitora cada vela concluída, identifica o pivô ZigZag mais recente usando a configuração clássica de profundidade, desvio e backstep e entra no mercado se o pivô ainda for recente. Uma oscilação alta aciona uma posição longa, enquanto uma oscilação baixa abre uma posição curta, correspondendo ao mapa de sinal EA original.

## Lógica de negociação
- Assine o tipo de vela configurado e alimente os indicadores Maior/Mínimo cujo comprimento corresponda ao parâmetro de profundidade do ZigZag. O par de indicadores emula a detecção nativa de oscilação do ZigZag sem depender de buffers personalizados.
- Quando uma vela fecha, verifique se sua máxima toca o máximo rastreado ou se sua mínima toca o mínimo rastreado. Mude para um novo pivô apenas se o desvio exigido nas etapas de preço for satisfeito e a distância de retrocesso (barras mínimas entre pivôs opostos) for respeitada.
- Depois que um pivô for registrado, continue contando quantas barras passaram. O parâmetro urgência define quantas barras após o pivô ainda são consideradas acionáveis. Sinais anteriores a este limite são ignorados, evitando entradas tardias.
- Para um pivô alto a estratégia se prepara para comprar, e para um pivô baixo ela se prepara para vender. Se uma posição aberta já corresponder à direção pretendida, o sinal será marcado como manipulado e nenhuma ordem adicional será enviada.
- Se a conta atualmente mantém exposição na direção oposta, a estratégia envia uma ordem de mercado para estabilizar antes de abrir uma nova negociação. Depois envia imediatamente uma ordem de mercado com o volume configurado para estabelecer a nova posição.
- Cada ação requer um estado de indicador totalmente formado, uma vela finalizada e um volume de negociação positivo. A estratégia verifica a conectividade e as permissões usando `IsFormedAndOnlineAndAllowTrading()` antes de interagir com o mercado, garantindo que as ordens sejam enviadas apenas em condições de negociação saudáveis.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `Depth` | Profundidade ZigZag que define a janela de detecção de swing. | 17 |
| `Deviation` | Movimento mínimo do preço em pontos necessários para confirmar o pivô do mesmo tipo. Convertido internamente em etapas de preços de instrumentos. | 7 |
| `Backstep` | Número mínimo de barras que devem passar antes de mudar para um pivô oposto. | 5 |
| `Urgency` | Número máximo de barras após um pivô durante as quais as negociações são permitidas. | 2 |
| `Candle Type` | Tipo de dados de vela (período de tempo ou agregação personalizada) usado para cálculos. | Período de 5 minutos |
| `Volume` | Volume de ordens de mercado enviadas em cada entrada. | 0,1 |

## Notas de implementação
- Os indicadores Mais Alto/Mais Baixo são limitados por meio do `SubscribeCandles().Bind()` API de alto nível, portanto, a estratégia opera apenas nas velas finais e evita o buffer manual.
- O parâmetro de desvio é transformado numa diferença de preço absoluta utilizando o passo de preço do instrumento. Se o símbolo não tiver metadados de variação de preço, o valor 1 será usado como substituto, mantendo a lógica consistente em todas as exchanges.
- Uma proteção booleana evita negociações duplicadas por pivô, correspondendo ao comportamento MetaTrader EA que atua apenas uma vez por swing.
- A integração de gráficos integrada desenha velas e executa negociações automaticamente quando o gráfico está disponível, o que ajuda a validar visualmente os pontos de oscilação e as entradas.
- A gestão de posições é simétrica: qualquer exposição oposta é nivelada com uma ordem de mercado de igual volume antes de estabelecer a nova negociação, mantendo a carteira unilateral como o consultor especialista original.
