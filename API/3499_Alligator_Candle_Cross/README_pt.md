# Alligator Estratégia Cruzada de Velas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia transporta os MetaTrader especialistas **cruz de vela de crocodilo para cima/para baixo** para o StockSharp API de alto nível. Ele monitora o indicador Bill Williams Alligator construído a partir de médias móveis suavizadas do preço médio e reage sempre que um corpo de vela completo viaja de um lado da boca Alligator para o outro. As entradas podem ser restritas a alta, baixa ou ambas as direções por meio de um parâmetro, enquanto paradas e metas fixas baseadas em pip cuidam do gerenciamento de risco.

## Lógica de negociação

### Preparação de indicadores
- Calcule os Alligator **Mandíbula**, **Dentes** e **Lábios** usando médias móveis suavizadas com os comprimentos clássicos 13/8/5.
- Aplique os tradicionais deslocamentos para frente (8/5/3 barras por padrão) para que cada linha seja comparada com a vela que se forma na frente dela.
- Todos os preços são amostrados a partir da mediana da vela `(High + Low) / 2` para corresponder à implementação de MetaTrader.

### Configuração longa ("vela cruzada")
1. A vela finalizada anterior deve fechar na linha Alligator inferior ou abaixo dela (após aplicar o deslocamento).
2. O corpo da vela atual abre no valor Alligator deslocado mais alto ou abaixo dele e fecha acima desse mesmo valor, provando que o corpo cruzou a boca Alligator na direção ascendente.
3. Nenhuma posição está aberta no momento e a negociação é permitida.
4. Quando todas as condições se alinham, a estratégia envia uma **Compra** de mercado para o volume configurado.

### Configuração curta ("vela cruzada para baixo")
1. O fechamento anterior deve estar igual ou acima da linha Alligator deslocada mais alta.
2. O corpo da vela atual abre no valor deslocado Alligator ou acima dele e termina abaixo dele, confirmando uma linha de baixa através do Alligator.
3. Nenhuma posição está aberta e a negociação está habilitada.
4. Uma ordem de **Venda** a mercado é enviada para o volume configurado.

### Gestão de posição
- Quando uma nova posição é aberta, a estratégia converte as distâncias de stop-loss e take-profit de pips em preços absolutos usando a etapa de preço do símbolo.
- As posições longas saem quando a vela toca o stop loss, atinge o alvo ou fecha abaixo do mínimo das linhas deslocadas de Teeth e Lips.
- As posições curtas saem no stop-loss, no alvo ou em um fechamento acima do máximo dos valores deslocados de Teeth e Lips.
- A chamada integrada **StartProtection** é ativada na inicialização para garantir que preenchimentos anormais sejam fechados com segurança.

## Parâmetros

| Nome | Tipo | Padrão | Descrição |
| ---- | ---- | ------- | ----------- |
| `OrderVolume` | `decimal` | `0.1` | Tamanho comercial em lotes ou contratos. |
| `StopLossPips` | `int` | `50` | Distância do preço de entrada até o stop de proteção em pips. Zero desativa a parada. |
| `TakeProfitPips` | `int` | `50` | Distância da entrada até a meta de lucro fixo em pips. Zero desativa o alvo. |
| `JawPeriod` | `int` | `13` | Comprimento médio móvel suavizado para a linha da mandíbula Alligator (azul). |
| `JawShift` | `int` | `8` | Deslocamento para frente aplicado à linha da mandíbula antes de avaliar os sinais. |
| `TeethPeriod` | `int` | `8` | Comprimento médio móvel suavizado para a linha Alligator dentes (vermelha). |
| `TeethShift` | `int` | `5` | Deslocamento para frente da linha dos dentes. |
| `LipsPeriod` | `int` | `5` | Comprimento médio móvel suavizado para a linha dos lábios Alligator (verde). |
| `LipsShift` | `int` | `3` | Deslocamento para frente da linha dos lábios. |
| `CandleType` | `DataType` | `TimeSpan.FromHours(1).TimeFrame()` | Série de velas usadas para cálculos. |
| `EntryMode` | `AlligatorCrossMode` | `Both` | Escolhe se a estratégia negocia configurações longas, configurações curtas ou ambas. |

## Notas de uso
- Funciona em qualquer instrumento suportado por StockSharp; certifique-se de que `CandleType` corresponda ao período usado no modelo MetaTrader original.
- Os pips são inferidos a partir da etapa de preço do instrumento: para 3 ou 5 cotações decimais (por exemplo, EURUSD), o pip equivale a dez etapas de preço.
- A lógica atua apenas em velas concluídas e não depende de dados de ticks, o que a mantém alinhada com backtests MetaTrader.
