# Estratégia Exp Multic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia multi-moeda que opera um conjunto fixo dos principais pares Forex sem indicadores técnicos.
Para cada par, o algoritmo mantém uma direção e um volume. Após cada movimento lucrativo o volume é aumentado; após uma perda, a direção é invertida. As operações são interrompidas e todas as posições são fechadas quando o lucro ou a perda global excede os limiares especificados.

## Detalhes

- **Critérios de entrada**:
  - Se não houver posição e o saldo da conta estiver acima de `Margin`, abre uma posição na direção predefinida com `MinVolume`.
- **Comprado/Vendido**: Ambos, dependendo da direção interna de cada par.
- **Critérios de saída**:
  - Fechar a posição quando o lucro exceder `KClose * MinVolume`.
  - Inverter a direção e fechar quando a perda exceder `KChange * volume atual`.
- **Stops**: Sem stops explícitos; o risco é controlado pelos limiares de lucro/perda.
- **Valores padrão**:
  - `Loss` = 1900
  - `Profit` = 4000
  - `Margin` = 5000
  - `MinVolume` = 0.01
  - `KChange` = 2100
  - `KClose` = 4600
- **Filtros**:
  - Categoria: Gestão monetária
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Baseado em ticks
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Alto
