# Estratégia Exp UltraFATL Duplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Exp UltraFATL Duplex** é uma conversão em C# do consultor especialista do MetaTrader 5 `Exp_UltraFatl_Duplex`. O sistema executa dois pipelines independentes do indicador UltraFATL: um dedicado a oportunidades compradas e outro ajustado para configurações vendidas. Cada pipeline avalia uma escada de valores FATL suavizados e conta quantas etapas estão subindo ou descendo. O equilíbrio entre os contadores altista e baixista define a direção da próxima operação.

## Lógica de trading
1. Assinar o período de candles configurado para cada bloco direcional.
2. Filtrar o preço aplicado com o kernel FATL (filtro digital de 39 coeficientes).
3. Alimentar a série filtrada por uma escada de médias móveis cujos comprimentos aumentam pelo passo configurado. A escada usa o método de suavização especificado pelo usuário.
4. Comparar valores consecutivos dentro da escada para contar votos altistas e baixistas. Suavizar ambos os contadores com uma segunda média móvel.
5. Avaliar os contadores no deslocamento de sinal selecionado (padrão: um candle completamente fechado):
   - O **bloco comprado** abre uma posição quando o candle anterior mostrou domínio altista, mas o candle atual mostra contadores cruzando para baixo (altistas ≤ baixistas). Fecha a posição comprada quando os baixistas superam os altistas no candle anterior.
   - O **bloco vendido** funciona na direção oposta: abre um vendido quando o candle anterior está dominado por baixistas e o candle atual cruza para cima (altistas ≥ baixistas). Fecha o vendido quando os altistas lideram no candle anterior.
6. Os níveis opcionais de stop-loss e take-profit são avaliados sobre dados de candles usando o passo de preço do instrumento.

A estratégia aplica uma posição líquida: sinais vendidos fecham comprados existentes antes de abrir, e vice-versa. Ordens de mercado são usadas para entradas e saídas.

## Parâmetros
### Bloco comprado
- **Long Volume** – tamanho da ordem ao abrir uma operação comprada.
- **Allow Long Entries** – habilitar ou desabilitar novas posições compradas.
- **Allow Long Exits** – permitir o fechamento de comprados em sinais opostos.
- **Long Candle Type** – período usado para o pipeline UltraFATL comprado.
- **Long Applied Price** – fonte de preço (fechamento, típico, DeMark, etc.) alimentada ao kernel FATL.
- **Long Trend Method / Start Length / Phase / Step / Steps** – configuração de suavização da escada.
- **Long Counter Method / Counter Length / Counter Phase** – configurações de suavização para os contadores altista/baixista.
- **Long Signal Bar** – número de candles completos usados como deslocamento de sinal (valores menores que 1 são tratados como 1).
- **Long Stop (pts)** – distância de stop-loss opcional em passos de preço.
- **Long Target (pts)** – distância de take-profit opcional em passos de preço.

### Bloco vendido
Configurações simétricas para o pipeline vendido: **Short Volume**, **Allow Short Entries**, **Allow Short Exits**, **Short Candle Type**, **Short Applied Price**, **Short Trend Method / Start Length / Phase / Step / Steps**, **Short Counter Method / Counter Length / Counter Phase**, **Short Signal Bar**, **Short Stop (pts)**, **Short Target (pts)**.

## Notas de implementação
- Os métodos de suavização são mapeados para indicadores do StockSharp. Opções baseadas em Jurik reutilizam `JurikMovingAverage`; métodos como `Parabolic` e `T3` são aproximados com médias móveis exponenciais ou Jurik porque os kernels personalizados originais não estão disponíveis.
- Os níveis de stop-loss e take-profit são avaliados sobre máximos/mínimos de candles; não são ordens de proteção do lado do servidor.
- Os deslocamentos de sinal menores que uma barra não podem ser reproduzidos porque o port do StockSharp reage apenas a candles terminados. Portanto, definir a barra de sinal como zero se comporta identicamente a um deslocamento de um.
- Ambos os pipelines de indicadores desenham seus contadores suavizados em áreas de gráfico dedicadas para inspeção visual.

## Uso
Adicionar a estratégia à sua solução StockSharp, configurar os blocos direcionais de acordo com seu plano de trading e executá-la dentro do Designer, Shell ou Runner. Certificar-se de que o instrumento fornece a série de candles necessária e que os parâmetros `LongVolume`/`ShortVolume` estão configurados com o tamanho de ordem desejado.
