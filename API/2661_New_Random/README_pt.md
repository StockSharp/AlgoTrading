# Estratégia Nova Aleatória
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Nova Aleatória** emula o especialista MetaTrader original "New Random" oferecendo três modos distintos de seleção de entrada. Ela abre apenas uma única posição por vez e aguarda até que a posição atual seja fechada antes de gerar a próxima direção de ordem. As entradas a mercado são acionadas em atualizações do melhor preço (dados Level 1) usando os melhores preços bid/ask como âncoras de execução. A estratégia calcula automaticamente os offsets de stop-loss e take-profit em pips, adaptando-se a cotações forex de 3 e 5 dígitos da mesma forma que a versão MQL.

## Modos de entrada
1. **Gerador** – a próxima direção é escolhida por um gerador pseudoaleatório semeado no início da estratégia. Cada oportunidade é um lançamento de moeda independente entre comprar e vender.
2. **Compra-Venda-Compra** – as posições alternam estritamente entre compra e venda. A primeira ordem é uma compra, seguida de uma venda, e assim por diante.
3. **Venda-Compra-Venda** – as posições alternam estritamente começando de uma venda, seguida de uma compra, e repetindo.

## Parâmetros
- **Random Mode** (`Mode`) – seleciona um dos três mecanismos de entrada descritos acima. Padrão: gerador aleatório.
- **Minimal Lot Count** (`MinimalLotCount`) – multiplica o volume mínimo negociável do instrumento. Um valor de `1` significa que a estratégia negocia exatamente `Security.VolumeMin`, enquanto valores maiores escalam o tamanho da ordem por múltiplos inteiros.
- **Stop Loss (pips)** (`StopLossPips`) – distância em pips abaixo/acima do preço de execução onde a estratégia sairá da posição. Definir como `0` para desabilitar o stop-loss.
- **Take Profit (pips)** (`TakeProfitPips`) – distância em pips onde a estratégia realizará lucros. Definir como `0` para desabilitar o take-profit.

## Lógica de negociação
1. Assina dados Level 1 para o ativo configurado e armazena constantemente os últimos preços bid, ask e último trade.
2. Quando não há posição aberta nem ordem pendente, a estratégia avalia o modo selecionado para determinar a próxima direção.
3. As ordens são colocadas a mercado usando o snapshot mais recente do melhor bid/ask. Os alvos de stop-loss e take-profit são calculados imediatamente a partir do preço de entrada usando os parâmetros de distância em pips.
4. Apenas uma posição pode existir por vez. Entradas subsequentes são suprimidas até que a posição ativa seja completamente fechada.

## Gestão de posição
- Posições compradas saem antecipadamente quando o preço atual cai ao stop-loss ou abaixo, ou sobe ao take-profit ou acima.
- Posições vendidas saem quando o preço atual sobe ao stop-loss ou acima, ou cai ao take-profit ou abaixo.
- As comparações de preço sempre usam as informações Level 1 mais recentes: o último preço de trade se disponível, caso contrário o melhor bid/ask para o lado respectivo.
- Após fechar um trade, a estratégia reinicia o estado interno, alterna opcionalmente a próxima direção (para modos de sequência) e aguarda a próxima atualização de cotação antes de reingressar.

## Notas
- A estratégia nunca piramidaliza posições e mantém o comportamento determinístico para os modos baseados em sequência.
- O modo aleatório é semeado com o contagem de ticks atual, portanto cada execução produz um fluxo de ordens único.
- Todos os comentários internos e logs estão em inglês para se alinharem com as diretrizes do repositório.
