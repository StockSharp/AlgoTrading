# Sistema Contra-Canal Donchain
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
O **Donchain Counter-Channel System** reproduz o consultor especialista MetaTrader 4 de 2005 de Michal Rutka. Ele observa os turnos em um canal Donchian de 20 dias calculado em velas diárias. Quando a banda inferior sobe, a estratégia assume que os vendedores não conseguiram empurrar o preço para novos mínimos e compram na próxima sessão no mercado. Quando a banda superior vira para baixo, a estratégia interpreta isso como uma perda de impulso nas altas e vende a descoberto no mercado. As paradas de proteção estão sempre alinhadas com a banda Donchian oposta para que as saídas espelhem a lógica original de gerenciamento de paradas.

É permitida apenas uma entrada a cada 24 horas, correspondendo à regra do artigo que restringe o sistema a no máximo um pedido por dia. Esta implementação usa StockSharp de alto nível API com ligações de indicadores para que os valores de Donchian cheguem junto com cada vela concluída.

## Lógica de negociação
1. Assine o `CandleType` configurado (diariamente por padrão) e avalie um indicador `DonchianChannels` com o `ChannelPeriod` selecionado.
2. Sempre que uma vela termina:
   - Se uma posição longa estiver aberta, mova o nível de stop para a banda inferior atual quando ela subir e saia se a mínima da vela tocar esse nível.
   - Se uma posição curta estiver aberta, mova o nível de stop para a banda superior atual quando ela cair e saia se a máxima da vela tocar esse nível.
   - Se não houver posição, pule as entradas quando a última negociação ocorreu há menos de `TradeCooldown`.
   - Opere comprado quando a banda inferior Donchian da vela anterior estiver mais alta do que a da vela anterior, sinalizando uma ascensão no fundo do canal. Defina a parada inicial para a banda inferior atual.
   - Opere vendido quando a banda superior Donchian da vela anterior estiver mais baixa do que a da vela anterior, sinalizando uma queda no teto do canal. Defina a parada inicial para a banda superior atual.
3. Continue seguindo o stop ao longo das bandas até que o preço reverta através delas, o que fecha a posição.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `Volume` | `1` | Tamanho do pedido para entradas longas e curtas. |
| `ChannelPeriod` | `20` | Número de velas usadas para calcular as bandas superior e inferior Donchian. |
| `TradeCooldown` | `1 day` | Período mínimo de espera antes que uma nova entrada seja permitida. |
| `CandleType` | `Daily` | Série de velas na qual o canal Donchian é calculado. |

## Indicadores e Dados
- **Donchian Canais** – fornece os limites superior e inferior do canal usados para detecção de mudança de tendência e para trailing stops.
- **Velas Diárias (padrão)** – horários de fechamento do suprimento necessários para o resfriamento de 24 horas e para avaliar os giros do indicador.

## Notas de implementação
- A estratégia usa `BindEx` para receber um `DonchianChannelsValue` digitado no manipulador de velas, garantindo que ambas as bandas estejam disponíveis simultaneamente.
- As paradas são simuladas monitorando os máximos e mínimos das velas em relação ao valor da banda armazenada, assim como o EA original atualizou seu stop-loss em cada nova barra.
- O temporizador de resfriamento é atualizado apenas em novas entradas, espelhando o script de origem que evitou múltiplas entradas no mesmo dia de negociação.
