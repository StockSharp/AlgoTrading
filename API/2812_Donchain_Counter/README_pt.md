# Estratégia de Contador Donchain
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Donchain Counter é uma portação para StockSharp do assessor especialista MQL5 "Donchain counter" de Michal Rutka. O sistema observa como o Canal Donchian se expande para detectar rompimentos e então defende a posição arrastando o stop ao longo da banda oposta assim que o preço se moveu uma distância fixa. Apenas uma posição pode ser aberta a cada 24 horas, espelhando a restrição original.

## Lógica de trading
### Entradas compradas
- Avalia sinais em velas completas do período configurado (padrão **H1**).
- Observa a banda superior do Donchian nas duas últimas barras fechadas. Quando a banda na barra *t-1* é maior do que na barra *t-2* (um rompimento recente da máxima do canal), uma ordem de compra a mercado é colocada.
- O stop protetor inicial é ancorado na banda inferior atual do Donchian.

### Entradas vendidas
- Monitora a banda inferior do Donchian nas duas últimas barras fechadas. Quando a banda na barra *t-1* é menor do que na barra *t-2* (um rompimento da mínima do canal), uma ordem de venda a mercado é enviada.
- O primeiro nível de stop é definido na banda superior atual do Donchian.

### Período de resfriamento entre trades
- Após qualquer nova entrada, o algoritmo registra o tempo de execução e bloqueia entradas subsequentes pela duração de `TradeCooldown` (padrão **24 horas**). Isso reproduz a regra de "apenas um trade por dia" na versão MQL.

### Regras de trailing e saída
- Um mecanismo de trailing só é ativado após o preço avançar pelo menos `BufferSteps` passos de preço além da banda Donchian oposta. Isso replica o requisito do EA original onde o mercado deve se mover 50 pontos antes que o stop seja apertado.
- Posições compradas: uma vez que o gatilho de trailing é acionado, o stop é atualizado para a banda inferior atual. Se a mínima da vela tocar nesse nível, a estratégia sai com uma ordem a mercado.
- Posições vendidas: após o gatilho ser acionado, o stop segue a banda superior atual. Se a máxima da vela atingir esse preço, a posição é fechada.
- Quando o trailing stop força uma saída, a estratégia não abre uma nova posição até que o próximo sinal e o período de resfriamento o permitam.

### Gestão de risco
- A estratégia sempre opera uma única posição cujo tamanho é definido pelo parâmetro `Volume`.
- Não há meta de lucro; todas as saídas são impulsionadas pela lógica de trailing do Donchian.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `Volume` | Tamanho de ordem para entradas. | `1` |
| `ChannelPeriod` | Período de retrospectiva para o cálculo do Canal Donchian. | `20` |
| `BufferSteps` | Número de passos de preço que o preço deve exceder além da banda oposta antes que o trailing seja ativado (MQL usou 50 pontos). | `50` |
| `TradeCooldown` | Tempo mínimo entre novas entradas. | `1 dia` |
| `CandleType` | Série de velas usada para o indicador (padrão velas de 1 hora). | `velas de 1h` |

## Indicadores
- **Canais Donchian** – as bandas superior e inferior definem sinais de rompimento e stops dinâmicos.

## Notas
- Use instrumentos com um `PriceStep` razoável para que o buffer se traduza em distância de preço realista. A estratégia usa por padrão um passo de 0.0001 se nenhum for fornecido pelo instrumento.
- Apenas uma direção fica aberta de cada vez. Antes de mudar de direção, a posição existente deve fechar completamente, assim como no assessor especialista original.
- Objetos de gráfico são preparados automaticamente se uma área de gráfico estiver disponível: velas, o canal Donchian e os próprios trades da estratégia.
