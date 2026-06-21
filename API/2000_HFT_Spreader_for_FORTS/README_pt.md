# HFT Spreader para FORTS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
Esta estratégia replica o comportamento de um spreader de alta frequência no mercado FORTS. Monitora continuamente o livro de ordens e coloca ordens limitadas em ambos os lados do mercado para capturar o diferencial bid-ask.

## Lógica da Estratégia
- Assinar atualizações em tempo real do livro de ordens.
- Quando não há posição aberta e o diferencial é amplo o suficiente (determinado por `SpreadMultiplier`), a estratégia coloca:
  - Uma ordem limitada de compra um tick acima do melhor bid.
  - Uma ordem limitada de venda um tick abaixo do melhor ask.
- Se existir uma posição e não houver ordens ativas, coloca uma única ordem limitada no lado oposto para fechar e reverter a posição.
- As ordens são canceladas e substituídas quando os melhores preços se movem para mantê-las no topo do livro.

## Parâmetros
- `SpreadMultiplier` – diferencial necessário em ticks para colocar ambas as ordens de compra e venda. O padrão é 4 ticks.
- `Volume` – volume da ordem. O padrão é 1 lote.

## Notas de Uso
- Desenvolvida para instrumentos com tamanhos de tick pequenos, como futuros na bolsa FORTS.
- Usa apenas ordens limitadas; nenhuma ordem de mercado é enviada exceto pelo mecanismo de proteção, se necessário.
- Garantir liquidez suficiente e ambiente de baixa latência para operação eficaz.
