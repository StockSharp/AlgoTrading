# Estratégia de Abertura e Fechamento no Horário Certo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Uma estratégia simples baseada em tempo que abre uma posição de mercado em um horário específico do dia e a fecha em outro horário predefinido. A direção (compra ou venda) e o volume da ordem são configuráveis. Este exemplo demonstra a execução programada de negociações sem usar indicadores ou filtros adicionais.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Em `Open Time` quando `Is Buy` está habilitado.
  - **Vendido**: Em `Open Time` quando `Is Buy` está desabilitado.
- **Comprado/Vendido**: Ambos, dependendo de `Is Buy`.
- **Critérios de saída**:
  - A posição é fechada em `Close Time` independentemente do lucro ou perda.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Open Time` = 13:00.
  - `Close Time` = 13:01.
  - `Volume` = 1.
  - `Is Buy` = true.
  - `Candle Type` = 1 minuto.
- **Filtros**:
  - Categoria: Tempo
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
