# Estratégia Operador de Linha Cruzada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia emula o expert original do MetaTrader "Cross Line Trader" reagindo a interações de preço com linhas sintéticas definidas pelo usuário. Em vez de ouvir objetos de gráfico manuais, a versão StockSharp recebe todas as descrições de linhas através de um único parâmetro, analisa-as na inicialização e monitora continuamente as velas terminadas. Quando a abertura de uma vela passa por uma linha ativa, a estratégia coloca uma ordem a mercado na direção correspondente e desativa essa linha para que não possa ser disparada novamente.

## Lógica de negociação
1. A estratégia assina o tipo de vela selecionado no parâmetro **Candle Type** e processa apenas velas no estado `Finished` para evitar ruído intrabarra.
2. As linhas sintéticas são criadas a partir do parâmetro **Line Definitions**. Cada linha mantém seu próprio estado (ativa/expirada, número de barras processadas e geometria).
3. Para linhas **Trend** ou **Horizontal**, o algoritmo compara a abertura da vela anterior com a seguinte em relação à trajetória de preço da linha:
   - Um sinal de compra ocorre quando a abertura anterior está abaixo da linha e a abertura atual sobe acima dela.
   - Um sinal de venda ocorre quando a abertura anterior está acima da linha e a abertura atual cai abaixo dela.
4. As linhas **Vertical** funcionam como gatilhos temporizados. Após o número configurado de barras, a estratégia abre uma posição imediatamente na abertura da vela atual.
5. A direção é determinada de acordo com **Direction Mode**:
   - `FromLabel` compara cada rótulo de linha com **Buy Label** e **Sell Label**.
   - `ForceBuy` e `ForceSell` tratam todas as linhas na mesma direção, independentemente dos rótulos.
6. Cada gatilho bem-sucedido envia uma ordem a mercado com o volume de **Trade Volume**, registra a ativação e marca a linha como inativa.
7. As distâncias opcionais de stop-loss e take-profit são aplicadas em cada nova vela avaliando o último preço de entrada contra as máximas e mínimas da vela.

## Formato de definição de linhas
A cadeia **Line Definitions** usa ponto e vírgula para separar entradas. Cada entrada deve seguir:

```
Name|Type|Label|BasePrice|SlopePerBar|Length|Ray
```

- **Name** – identificador exibido nos registros. Qualquer string sem ponto e vírgula.
- **Type** – `Horizontal`, `Trend` ou `Vertical` (sem distinção de maiúsculas/minúsculas).
- **Label** – texto livre usado quando **Direction Mode** é `FromLabel`.
- **BasePrice** – preço inicial da linha na primeira vela processada. Necessário para cada linha não vertical (decimal, cultura invariante).
- **SlopePerBar** – variação de preço por vela para uma linha de tendência. Use `0` para linhas horizontais.
- **Length** – o significado depende do tipo de linha:
  - Para linhas de tendência ou horizontais sem ray, define quantas barras a âncora direita está do início. Após essa contagem, a linha expira automaticamente.
  - Para linhas ray, o valor é ignorado porque a linha se estende indefinidamente.
  - Para linhas verticais, especifica quantas barras aguardar antes de disparar. O valor mínimo aceito é `1`.
- **Ray** – `true` mantém a linha ativa indefinidamente à direita, `false` a restringe ao comprimento especificado.

Exemplo:

```
TrendLine|Trend|Buy|1.1000|0.0005|8|false;HorizontalSell|Horizontal|Sell|1.1050|0|0|true;VerticalImpulse|Vertical|Buy|0|0|1|false
```

O exemplo cria uma linha de tendência de compra ascendente, um nível horizontal de venda que nunca expira e um gatilho vertical único para a próxima vela.

## Parâmetros
- **Candle Type** – tipo de dado de mercado usado para cálculos. Padrão: período de 1 minuto.
- **Trade Volume** – tamanho da ordem para novas entradas. Deve ser positivo.
- **Direction Mode** – determina como o lado de entrada é selecionado (`FromLabel`, `ForceBuy`, `ForceSell`).
- **Buy Label** / **Sell Label** – valores de rótulo para identificar linhas quando **Direction Mode** é `FromLabel`.
- **Line Definitions** – string bruta que descreve cada linha sintética (ver formato acima).
- **Stop Loss Offset** – distância em unidades de preço para saídas de proteção em posições compradas e vendidas (0 desabilita a verificação).
- **Take Profit Offset** – distância de preço para metas de lucro (0 desabilita a verificação).

## Gestão de risco
A estratégia não coloca ordens de stop ou take-profit separadas. Em vez disso, monitora cada vela terminada:
- Posições compradas fecham se a mínima da vela viola `EntryPrice - StopLossOffset` ou a máxima excede `EntryPrice + TakeProfitOffset`.
- Posições vendidas fecham se a máxima da vela viola `EntryPrice + StopLossOffset` ou a mínima cai abaixo de `EntryPrice - TakeProfitOffset`.

Se ambos os offsets forem zero, a posição só será fechada pelo sinal oposto ou intervenção manual.

## Notas de implementação
- Todos os comentários no código-fonte estão em inglês para manter a consistência com as diretrizes do projeto.
- A estratégia ignora silenciosamente definições de linha inválidas; certifique-se de que o formato esteja correto para evitar gatilhos perdidos.
- Reiniciar a estratégia limpa o estado interno, portanto os contadores de linha e os temporizadores de ativação começam novamente a partir da primeira vela processada.
- A abordagem foca nos preços de abertura das velas, assim como o EA original, e não reagirá a toques intrabarra.

## Uso
1. Configurar o instrumento de negociação e o tipo de vela desejado.
2. Ajustar **Line Definitions** para descrever cada linha manual com a qual deseja negociar.
3. Configurar **Direction Mode** para confiar em rótulos ou forçar negociação unidirecional.
4. Opcionalmente definir offsets de stop-loss e take-profit para saídas automáticas.
5. Iniciar a estratégia e monitorar os registros: cada linha disparada é reportada junto com sua direção e preço de ativação.
