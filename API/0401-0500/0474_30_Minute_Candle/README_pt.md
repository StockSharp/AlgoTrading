# Estratégia de Vela de 30 Minutos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta abordagem compara o preço de abertura da vela atual de 30 minutos com o fechamento da vela anterior.
Se uma nova vela abre acima do fechamento anterior, uma posição comprada é aberta.
Quando já estiver comprado e a próxima vela abrir abaixo do fechamento anterior, a estratégia inverte para uma posição vendida.
Todas as posições abertas são fechadas um minuto antes de a vela atual terminar.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: abertura da vela atual > fechamento da vela anterior.
  - **Vendido**: abertura da vela atual < fechamento da vela anterior enquanto mantém uma posição comprada.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Fechar qualquer posição um minuto antes de a vela fechar.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `CandleType` = TimeSpan.FromMinutes(30).TimeFrame().
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: Price action
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
