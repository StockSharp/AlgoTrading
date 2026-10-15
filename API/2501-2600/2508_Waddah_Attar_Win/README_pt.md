# Estratégia Waddah Attar Win
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia espelha o consultor especializado original Waddah Attar Win. Ela mantém continuamente uma grade simétrica de ordens limite de compra e venda espaçadas a um número fixo de pontos do bid/ask atual. Sempre que o preço de mercado se aproxima da última ordem enviada, a estratégia empilha um novo limite à mesma distância com um incremento de volume opcional. O lucro flutuante é monitorado a cada atualização do livro de ordens e todas as posições junto com ordens pendentes são fechadas assim que o alvo de lucro configurado na moeda da conta é atingido.

## Detalhes

- **Critérios de entrada**:
  - Buy-limit inicial colocado `Step Points` abaixo do bid e sell-limit colocado a mesma distância acima do ask.
  - Ordens pendentes adicionais são adicionadas quando o preço fica dentro de cinco passos de preço da última ordem em cada lado.
- **Comprado/Vendido**: Ambos, grade com hedge.
- **Critérios de saída**:
  - Fechar todas as posições e cancelar ordens assim que o patrimônio exceder o saldo armazenado em `Min Profit`.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Step Points` = 20
  - `First Volume` = 0.1
  - `Increment Volume` = 0.0
  - `Min Profit` = 910
- **Notas**:
  - Funciona com portfólios de hedge porque posições compradas e vendidas podem coexistir.
  - Usa dados do livro de ordens para reagir imediatamente a mudanças no bid/ask.
